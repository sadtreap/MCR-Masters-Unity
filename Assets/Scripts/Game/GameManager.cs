using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using System.Collections;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TMPro;

using MCRGame.Common;
using MCRGame.UI;
using MCRGame.Net;
using MCRGame.View;
using MCRGame.Audio;


namespace MCRGame.Game
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        // 게임 관련 데이터
        public List<Player> Players { get; private set; }
        public AbsoluteSeat MySeat { get; private set; }
        public RelativeSeat CurrentTurnSeat { get; private set; }
        public Round CurrentRound { get; private set; }

        // Inspector에서 할당하는 GameHandManager 오브젝트를 통해 GameHand를 관리합니다.
        [SerializeField]
        private GameHandManager gameHandManager;
        public GameHandManager GameHandManager => gameHandManager;
        public GameHand GameHand => gameHandManager != null ? gameHandManager.GameHandPublic : null;

        public bool IsFlowerConfirming = false;

        [SerializeField]
        private DiscardManager discardManager;

        public bool IsRightClickTsumogiri;

        public const int MAX_TILES = 144;
        public const int MAX_PLAYERS = 4;
        private int leftTiles;
        [SerializeField]
        private TextMeshProUGUI leftTilesText;

        [SerializeField]
        private TextMeshProUGUI currentRoundText;

        // 추가: Inspector에서 할당할 수 있는 4개의 Hand3DField 배열 (index 0~3 은 각 상대 좌석에 대응)
        [SerializeField]
        private Hand3DField[] playersHand3DFields;
        [SerializeField]
        private CallBlockField[] callBlockFields;

        public Dictionary<AbsoluteSeat, int> seatToPlayerIndex;
        private Dictionary<int, AbsoluteSeat> playerIndexToSeat;
        private Dictionary<string, int> playerUidToIndex;

        [Header("Camera")]
        [SerializeField] private CameraResultAnimator cameraResultAnimator;

        [Header("Score Label Texts (RelativeSeat 순서)")]
        [SerializeField] private TextMeshProUGUI scoreText_Self;
        [SerializeField] private TextMeshProUGUI scoreText_Shimo;
        [SerializeField] private TextMeshProUGUI scoreText_Toi;
        [SerializeField] private TextMeshProUGUI scoreText_Kami;

        [Header("Score Colors")]
        [Tooltip("양의 점수일 때 텍스트 색상")]
        [SerializeField] private Color positiveScoreColor = new Color(0f, 0.0941f, 0.7373f); // #0018CB
        [Tooltip("0점일 때 텍스트 색상")]
        [SerializeField] private Color zeroScoreColor = Color.white;                  // #FFFFFF
        [Tooltip("음의 점수일 때 텍스트 색상")]
        [SerializeField] private Color negativeScoreColor = new Color(0.7804f, 0.7569f, 0.3186f); // #C7C151

        public Color PositiveScoreColor => positiveScoreColor;
        public Color ZeroScoreColor => zeroScoreColor;
        public Color NegativeScoreColor => negativeScoreColor;

        [Header("Wind Label Texts (RelativeSeat 순서)")]
        [SerializeField] private TextMeshProUGUI windText_Self;
        [SerializeField] private TextMeshProUGUI windText_Shimo;
        [SerializeField] private TextMeshProUGUI windText_Toi;
        [SerializeField] private TextMeshProUGUI windText_Kami;

        [Header("Wind Colors")]
        [Tooltip("동풍(E)일 때 텍스트 색상")]
        [SerializeField] private Color eastWindColor = new Color(0.7961f, 0f, 0f); // #CB0000
        [Tooltip("나머지 풍향일 때 텍스트 색상")]
        [SerializeField] private Color otherWindColor = Color.black;            // #000000

        [Header("Profile UI (SELF, SHIMO, TOI, KAMI 순서)")]
        [SerializeField] private Image[] profileImages = new Image[4];
        [SerializeField] private Image[] profileFrameImages = new Image[4];
        [SerializeField] private Image[] BlinkTurnImages = new Image[4];
        [SerializeField] private TextMeshProUGUI[] nicknameTexts = new TextMeshProUGUI[4];
        [SerializeField] private Image[] flowerImages = new Image[4];
        [SerializeField] private TextMeshProUGUI[] flowerCountTexts = new TextMeshProUGUI[4];

        [SerializeField] private Sprite FlowerIcon_White;
        [SerializeField] private Sprite FlowerIcon_Yellow;
        [SerializeField] private Sprite FlowerIcon_Red;

        private Dictionary<RelativeSeat, int> flowerCountMap = new();

        [Header("Effect Prefabs")]
        [SerializeField] private GameObject flowerPhaseEffectPrefab;
        [SerializeField] private GameObject roundStartEffectPrefab;


        // Inspector에서 할당할 기본 프레임
        [Header("Default Profile Frame/Image")]
        [SerializeField] private Sprite defaultFrameSprite;
        [SerializeField] private Sprite defaultProfileImageSprite;

        [Header("Tsumo Action UI")]
        [SerializeField] private RectTransform actionButtonPanel;    // 버튼을 담을 패널 (GridLayoutGroup 붙임)
        [SerializeField] private GameObject actionButtonPrefab;      // 버튼 프리팹
        [SerializeField] private Sprite skipButtonSprite;
        [SerializeField] private Sprite chiiButtonSprite;
        [SerializeField] private Sprite ponButtonSprite;
        [SerializeField] private Sprite kanButtonSprite;
        [SerializeField] private Sprite huButtonSprite;
        [SerializeField] private Sprite flowerButtonSprite;
        [SerializeField] private GameObject backButtonPrefab;

        [Header("Timer UI")]
        [SerializeField] private TextMeshProUGUI timerText;
        private float remainingTime;
        private int currentActionId;

        private bool isGameStarted = false;

        public bool IsMyTurn;

        public RelativeSeat currentTurnSeat;

        [SerializeField] private GameObject EndScorePopupPrefab;

        public bool isInitHandDone = false;
        public List<GameWSMessage> pendingFlowerReplacement = new();

        private int prevBlinkSeat = -1;

        public bool isActionUIActive = false;

        public bool isAfterTsumoAction = false;
        
        public bool CanClick = false;
        public void UpdateCurrentTurnEffect()
        {
            int currentIndex = (int)currentTurnSeat;

            // 이전 효과 정지
            if (prevBlinkSeat >= 0 && prevBlinkSeat < profileFrameImages.Length)
            {
                var oldBlink = BlinkTurnImages[prevBlinkSeat];
                var oldEffect = oldBlink.GetComponent<BlinkTurnEffect>();
                oldEffect?.BlinkEffectOff();
            }

            // 새 효과 실행
            if (currentIndex >= 0 && currentIndex < profileFrameImages.Length)
            {
                var newBlink = BlinkTurnImages[currentIndex];
                var newEffect = newBlink.GetComponent<BlinkTurnEffect>();
                newEffect?.BlinkEffectOn();
            }

            prevBlinkSeat = currentIndex;
        }
        public void ResetAllBlinkTurnEffects()
        {
            foreach (var imgObj in BlinkTurnImages)
            {
                if (imgObj == null) continue;

                var effect = imgObj.GetComponent<BlinkTurnEffect>();
                if (effect != null)
                {
                    effect.BlinkEffectOff();  // → 알파값도 0 처리됨
                }
            }
            prevBlinkSeat = -1;
        }
        public IEnumerator WaitAndProcessTsumo(JObject data)
        {
            // GameManager 가 아직 뜨지 않은 경우도 고려
            yield return new WaitUntil(() =>
                !IsFlowerConfirming);

            ProcessTsumoActions(data);
        }

        public void UpdatePlayerScores(List<int> playersScores)
        {
            for (int i = 0; i < playersScores.Count; ++i)
            {
                Players[i].Score = playersScores[i];
            }
        }

        private void moveTurn(RelativeSeat seat)
        {
            if (seat == RelativeSeat.SELF)
            {
                IsMyTurn = true;
                CanClick = true;
            }
            else
            {
                IsMyTurn = false;
                CanClick = false;
            }
            currentTurnSeat = seat;
            UpdateCurrentTurnEffect();
            Debug.Log($"Current turn: {currentTurnSeat.ToString()}");
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            SetUIActive(false);
            ClearActionUI();
            isGameStarted = false;
        }

        void Update()
        {
            UpdateTimerText();
        }


        /// <summary>
        /// TileManager 클릭 시 호출: 서버로 검증 요청
        /// </summary>
        public void RequestDiscard(GameTile tile, bool is_tsumogiri)
        {
            var payload = new
            {
                event_type = (int)GameEventType.DISCARD,
                data = new
                {
                    tile = (int)tile,
                    is_tsumogiri,
                }
            };
            GameWS.Instance.SendGameEvent(GameWSActionType.GAME_EVENT, payload);
        }
        public void UpdateActionId(int actionId)
        {
            currentActionId = actionId;
        }
        public void SetTimer(object data)
        {
            try
            {
                // 1) data를 JObject로 변환
                var jData = data as JObject;
                if (jData == null)
                {
                    Debug.LogError("[SetTimer] data가 JObject가 아닙니다.");
                    return;
                }

                currentActionId = jData.Value<int>("action_id");
                remainingTime = jData.Value<float>("remaining_time");

                // 3) 파싱된 값 사용 예시
                Debug.Log($"[SetTimer] action_id: {currentActionId}, remaining_time: {remainingTime}");

                if (timerText != null)
                {
                    timerText.gameObject.SetActive(remainingTime > 0f);
                    timerText.text = Mathf.FloorToInt(remainingTime).ToString();
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SetTimer] JSON 파싱 중 오류: {ex.Message}");
            }
        }


        /// <summary>
        /// 서버에서 전달한 호출(CallBlock) 데이터를 파싱합니다.
        /// data는 JSON 형태로 { "seat": <seat>, "call_block_data": <CallBlockData JSON> }를 포함합니다.
        /// </summary>
        public void ConfirmCallBlock(object data)
        {
            ClearActionUI();
            try
            {
                Debug.Log("ConfirmCallBlock: Step 1 - Casting data to JObject");
                JObject jData = data as JObject;
                if (jData == null)
                {
                    Debug.LogWarning("ConfirmCallBlock: Data is not a valid JObject");
                    return;
                }

                Debug.Log("ConfirmCallBlock: Step 2 - Parsing 'seat' value");
                int seatInt = jData["seat"].ToObject<int>();
                Debug.Log("ConfirmCallBlock: seatInt = " + seatInt);
                AbsoluteSeat seat = (AbsoluteSeat)seatInt;
                Debug.Log("ConfirmCallBlock: seat = " + seat.ToString());

                Debug.Log("ConfirmCallBlock: Step 3 - Parsing 'call_block_data'");
                JToken callBlockToken = jData["call_block_data"];
                CallBlockData callBlockData = null;
                if (callBlockToken != null && callBlockToken.Type != JTokenType.Null)
                {
                    Debug.Log("ConfirmCallBlock: call_block_data token exists, type: " + callBlockToken.Type);
                    callBlockData = callBlockToken.ToObject<CallBlockData>();
                    if (callBlockData != null)
                    {
                        Debug.Log("ConfirmCallBlock: callBlockData parsed successfully. FirstTile = " + callBlockData.FirstTile.ToString());
                    }
                    else
                    {
                        Debug.LogWarning("ConfirmCallBlock: callBlockData parsed as null.");
                    }
                }
                else
                {
                    Debug.Log("ConfirmCallBlock: 'call_block_data' is null.");
                }

                // call block audio
                ActionAudioManager.Instance?.EnqueueCallSound(callBlockData.Type);

                Debug.Log("ConfirmCallBlock: Step 4 - Parsing 'has_tsumo_tile'");
                bool has_tsumo_tile = false;
                JToken hasTsumoTileToken = jData["has_tsumo_tile"];
                if (hasTsumoTileToken != null)
                {
                    Debug.Log("ConfirmCallBlock: has_tsumo_tile token exists.");
                }
                else
                {
                    Debug.Log("ConfirmCallBlock: has_tsumo_tile token is null.");
                }
                if (hasTsumoTileToken != null && callBlockData != null &&
                    (callBlockData.Type == CallBlockType.AN_KONG || callBlockData.Type == CallBlockType.SHOMIN_KONG))
                {
                    has_tsumo_tile = hasTsumoTileToken.ToObject<bool>();
                    Debug.Log("ConfirmCallBlock: has_tsumo_tile parsed as " + has_tsumo_tile);
                }
                else
                {
                    Debug.Log("ConfirmCallBlock: Skipping has_tsumo_tile parsing.");
                }

                Debug.Log("ConfirmCallBlock: Step 5 - Logging parsed values");
                Debug.Log($"ConfirmCallBlock: seat = {seat}, callBlockData = {(callBlockData != null ? callBlockData.FirstTile.ToString() : "null")}");

                Debug.Log("ConfirmCallBlock: Step 6 - Determining relative seat");
                RelativeSeat relativeSeat = RelativeSeatExtensions.CreateFromAbsoluteSeats(currentSeat: MySeat, targetSeat: seat);
                Debug.Log("ConfirmCallBlock: relativeSeat = " + relativeSeat);


                Debug.Log("ConfirmCallBlock: Step 7 - Accessing callBlockData.SourceSeat");
                RelativeSeat CallBlockSourceSeat = RelativeSeat.SELF;
                if (callBlockData != null)
                {
                    CallBlockSourceSeat = callBlockData.SourceSeat;
                }
                AbsoluteSeat sourceAbsoluteSeat = CallBlockSourceSeat.ToAbsoluteSeat(mySeat: seat);
                Debug.Log("ConfirmCallBlock: sourceAbsoluteSeat = " + sourceAbsoluteSeat);
                RelativeSeat sourceRelativeSeat = RelativeSeatExtensions.CreateFromAbsoluteSeats(currentSeat: MySeat, targetSeat: sourceAbsoluteSeat);
                Debug.Log("ConfirmCallBlock: sourceRelativeSeat = " + sourceRelativeSeat);

                if (relativeSeat == RelativeSeat.SELF)
                {
                    Debug.Log("ConfirmCallBlock: Applying call for SELF.");
                    gameHandManager.ApplyCall(cbData: callBlockData);
                }
                else
                {
                    Debug.Log("ConfirmCallBlock: Adding call block to callBlockFields for relativeSeat: " + relativeSeat);
                    if (callBlockData != null)
                    {
                        callBlockFields[(int)relativeSeat].AddCallBlock(data: callBlockData);
                    }
                    if (callBlockData.Type == CallBlockType.CHII || callBlockData.Type == CallBlockType.PUNG)
                    {
                        Debug.Log("ConfirmCallBlock: Starting RequestDiscardMultiple(count: 2)");
                        StartCoroutine(playersHand3DFields[(int)relativeSeat].RequestDiscardMultiple(count: 2));
                    }
                    else if (callBlockData.Type == CallBlockType.DAIMIN_KONG)
                    {
                        Debug.Log("ConfirmCallBlock: Starting RequestDiscardMultiple(count: 3)");
                        StartCoroutine(playersHand3DFields[(int)relativeSeat].RequestDiscardMultiple(count: 3));
                    }
                    else if (callBlockData.Type == CallBlockType.AN_KONG)
                    {
                        if (has_tsumo_tile)
                        {
                            Debug.Log("ConfirmCallBlock: AN_KONG with tsumo tile, starting RequestDiscardRightmost and RequestDiscardMultiple(count: 3)");
                            StartCoroutine(playersHand3DFields[(int)relativeSeat].RequestDiscardRightmost());
                            StartCoroutine(playersHand3DFields[(int)relativeSeat].RequestDiscardMultiple(count: 3));
                        }
                        else
                        {
                            Debug.Log("ConfirmCallBlock: AN_KONG without tsumo tile, starting RequestDiscardMultiple(count: 4)");
                            StartCoroutine(playersHand3DFields[(int)relativeSeat].RequestDiscardMultiple(count: 4));
                        }
                    }
                    else if (callBlockData.Type == CallBlockType.SHOMIN_KONG)
                    {
                        if (has_tsumo_tile)
                        {
                            Debug.Log("ConfirmCallBlock: SHOMIN_KONG with tsumo tile, starting RequestDiscardRightmost");
                            StartCoroutine(playersHand3DFields[(int)relativeSeat].RequestDiscardRightmost());
                        }
                        else
                        {
                            Debug.Log("ConfirmCallBlock: SHOMIN_KONG without tsumo tile, starting RequestDiscardMultiple(count: 1)");
                            StartCoroutine(playersHand3DFields[(int)relativeSeat].RequestDiscardMultiple(count: 1));
                        }
                    }
                }
                if (callBlockData.Type == CallBlockType.CHII ||
                    callBlockData.Type == CallBlockType.PUNG ||
                    callBlockData.Type == CallBlockType.DAIMIN_KONG)
                {
                    Debug.Log("ConfirmCallBlock: Removing last discard for sourceRelativeSeat = " + sourceRelativeSeat);
                    discardManager.RemoveLastDiscard(seat: sourceRelativeSeat);
                }

                moveTurn(relativeSeat);
            }
            catch (Exception ex)
            {
                Debug.LogError("ConfirmCallBlock parsing error: " + ex.Message);
            }
        }


        public void ConfirmTsumo(JObject data)
        {
            ClearActionUI();
            AbsoluteSeat TsumoSeat = (AbsoluteSeat)data["seat"].ToObject<int>();
            UpdateLeftTilesByDelta(-1);
            if (TsumoSeat == MySeat)
            {
                return;
            }

            RelativeSeat relativeTsumoSeat = RelativeSeatExtensions.CreateFromAbsoluteSeats(currentSeat: MySeat, targetSeat: TsumoSeat);

            moveTurn(seat: relativeTsumoSeat);

            if (playersHand3DFields[(int)relativeTsumoSeat].handTiles.Count >= GameHandManager.FULL_HAND_SIZE)
            {
                return;
            }
            StartCoroutine(playersHand3DFields[(int)relativeTsumoSeat].RequestTsumo());
        }
        public IEnumerator ConfirmFlower(JObject data)
        {
            ClearActionUI();
            GameTile floweredTile = (GameTile)data["tile"].ToObject<int>();
            AbsoluteSeat floweredSeat = (AbsoluteSeat)data["seat"].ToObject<int>();
            RelativeSeat floweredRelativeSeat = RelativeSeatExtensions.CreateFromAbsoluteSeats(currentSeat: MySeat, targetSeat: floweredSeat);

            ActionAudioManager.Instance?.EnqueueFlowerSound();

            if (floweredRelativeSeat == RelativeSeat.SELF)
            {
                bool animateDone = false;
                yield return gameHandManager.RunExclusive(gameHandManager.ApplyFlower(tile: floweredTile));
                int currentFlowerCount = flowerCountMap[floweredRelativeSeat];
                int previousCount = currentFlowerCount;
                currentFlowerCount++;


                StartCoroutine(AnimateFlowerCount(floweredRelativeSeat, previousCount, currentFlowerCount, () => { animateDone = true; }));
                yield return new WaitUntil(() => animateDone);
                SetFlowerCount(floweredRelativeSeat, currentFlowerCount);
            }
            else
            {
                // 상대의 경우: Hand3DField를 이용해 요청
                Hand3DField handField = playersHand3DFields[(int)floweredRelativeSeat];
                int currentFlowerCount = flowerCountMap[floweredRelativeSeat];
                int previousCount = currentFlowerCount;
                currentFlowerCount++;

                bool animateDone = false;

                // 동시에 꽃 카운트 애니메이션 실행
                StartCoroutine(AnimateFlowerCount(floweredRelativeSeat, previousCount, currentFlowerCount, () => { animateDone = true; }));
                // Hand3DField의 RequestDiscardRandom과 RequestInitFlowerTsumo를 순차 실행하는 코루틴
                yield return StartCoroutine(handField.RequestDiscardRandom());
                yield return new WaitUntil(() => animateDone);

                SetFlowerCount(floweredRelativeSeat, currentFlowerCount);
            }
            IsFlowerConfirming = false;
        }

        public void ConfirmDiscard(JObject data)
        {
            // discard sound audio
            DiscardSoundManager.Instance.PlayDiscardSound();

            Debug.Log($"[GameManager.ConfirmDiscard] discard tile successfully");
            ClearActionUI();
            GameTile discardTile = (GameTile)data["tile"].ToObject<int>();
            AbsoluteSeat discardedSeat = (AbsoluteSeat)data["seat"].ToObject<int>();
            bool is_tsumogiri = data["is_tsumogiri"].ToObject<bool>();
            if (discardedSeat == MySeat)
            {
                gameHandManager.ConfirmDiscard(tile: discardTile);
            }
            else
            {
                RelativeSeat enemyDiscardSeat = RelativeSeatExtensions.CreateFromAbsoluteSeats(currentSeat: MySeat, targetSeat: discardedSeat);
                if (is_tsumogiri)
                {
                    StartCoroutine(playersHand3DFields[(int)enemyDiscardSeat].RequestDiscardRightmost());
                }
                else
                {
                    StartCoroutine(playersHand3DFields[(int)enemyDiscardSeat].RequestDiscardRandom());
                }
                discardManager.DiscardTile(seat: enemyDiscardSeat, tile: discardTile);
            }
        }

        private void UpdateTimerText()
        {
            if (!timerText.IsActive())
                return;
            if (remainingTime > 0f)
            {
                remainingTime -= Time.deltaTime;
                int sec = Mathf.Max(0, Mathf.FloorToInt(remainingTime));
                if (timerText != null) timerText.text = sec.ToString();
                if (remainingTime <= 0f) timerText.text = "0";
            }
        }

        private GameObject additionalChoicesContainer;


        public void ReloadDiscardActions(List<GameAction> list)
        {
            ClearActionUI();

            isAfterTsumoAction = false;
            if (timerText != null)
            {
                timerText.gameObject.SetActive(remainingTime > 0f);
                timerText.text = Mathf.FloorToInt(remainingTime).ToString();
            }

            list.Sort();

            // 3) SKIP 버튼 (항상 제일 먼저)
            if (list.Count > 0)
            {
                isActionUIActive = true;

                var skip = Instantiate(actionButtonPrefab, actionButtonPanel);
                skip.GetComponent<Image>().sprite = skipButtonSprite;
                skip.GetComponent<Button>().onClick.AddListener(OnSkipButtonClicked);
            }

            // 4) CHII/KAN 그룹별 분기
            var groups = list.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var kv in groups)
            {
                var type = kv.Key;
                var actionsOfType = kv.Value;

                // CHII 또는 KAN 이고 선택지가 2개 이상이면 "추가 선택지" 버튼 생성
                if ((type == GameActionType.CHII || type == GameActionType.KAN)
                    && actionsOfType.Count > 1)
                {
                    var button = Instantiate(actionButtonPrefab, actionButtonPanel);
                    button.GetComponent<Image>().sprite = GetSpriteForAction(type);
                    button.GetComponent<Button>().onClick.AddListener(() =>
                        ShowAdditionalActionChoices(type, actionsOfType));
                }
                else
                {
                    // 단일 혹은 그 외 행동: 바로 버튼 생성
                    foreach (var act in actionsOfType)
                    {
                        var btnObj = Instantiate(actionButtonPrefab, actionButtonPanel);
                        btnObj.GetComponent<Image>().sprite = GetSpriteForAction(act.Type);
                        btnObj.GetComponent<Button>().onClick.AddListener(() =>
                            OnActionButtonClicked(act));
                    }
                }
            }
        }

        public void ProcessDiscardActions(JObject data)
        {
            ClearActionUI();

            isAfterTsumoAction = false;

            // 1) action_id, 남은 시간 초기화
            currentActionId = data["action_id"].ToObject<int>();
            remainingTime = data["left_time"].ToObject<float>();
            if (timerText != null)
            {
                timerText.gameObject.SetActive(remainingTime > 0f);
                timerText.text = Mathf.FloorToInt(remainingTime).ToString();
            }

            // 2) GameAction 리스트로 변환 후 정렬
            var list = data["actions"].ToObject<List<GameAction>>();
            list.Sort();

            // 3) SKIP 버튼 (항상 제일 먼저)
            if (list.Count > 0)
            {
                isActionUIActive = true;

                var skip = Instantiate(actionButtonPrefab, actionButtonPanel);
                skip.GetComponent<Image>().sprite = skipButtonSprite;
                skip.GetComponent<Button>().onClick.AddListener(OnSkipButtonClicked);
            }

            // 4) CHII/KAN 그룹별 분기
            var groups = list.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var kv in groups)
            {
                var type = kv.Key;
                var actionsOfType = kv.Value;

                // CHII 또는 KAN 이고 선택지가 2개 이상이면 "추가 선택지" 버튼 생성
                if ((type == GameActionType.CHII || type == GameActionType.KAN)
                    && actionsOfType.Count > 1)
                {
                    var button = Instantiate(actionButtonPrefab, actionButtonPanel);
                    button.GetComponent<Image>().sprite = GetSpriteForAction(type);
                    button.GetComponent<Button>().onClick.AddListener(() =>
                        ShowAdditionalActionChoices(type, actionsOfType));
                }
                else
                {
                    // 단일 혹은 그 외 행동: 바로 버튼 생성
                    foreach (var act in actionsOfType)
                    {
                        var btnObj = Instantiate(actionButtonPrefab, actionButtonPanel);
                        btnObj.GetComponent<Image>().sprite = GetSpriteForAction(act.Type);
                        btnObj.GetComponent<Button>().onClick.AddListener(() =>
                            OnActionButtonClicked(act));
                    }
                }
            }
        }

        public void ReloadTsumoActions(List<GameAction> list)
        {
            ClearActionUI();
            isAfterTsumoAction = true;
            if (timerText != null)
            {
                timerText.gameObject.SetActive(remainingTime > 0f);
                timerText.text = Mathf.FloorToInt(remainingTime).ToString();
            }

            list.Sort();


            // Skip 버튼
            if (list.Count > 0)
            {
                isActionUIActive = true;
                var skip = Instantiate(actionButtonPrefab, actionButtonPanel);
                skip.GetComponent<Image>().sprite = skipButtonSprite;
                skip.GetComponent<Button>().onClick.AddListener(OnSkipButtonClickedAfterTsumo);
            }

            // CHII/KAN 추가선택지 로직
            var groups = list.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var kv in groups)
            {
                var type = kv.Key;
                var actionsOfType = kv.Value;

                if ((type == GameActionType.CHII || type == GameActionType.KAN)
                    && actionsOfType.Count > 1)
                {
                    var button = Instantiate(actionButtonPrefab, actionButtonPanel);
                    button.GetComponent<Image>().sprite = GetSpriteForAction(type);
                    button.GetComponent<Button>().onClick.AddListener(() =>
                        ShowAdditionalActionChoices(type, actionsOfType));
                }
                else
                {
                    foreach (var act in actionsOfType)
                    {
                        var btnObj = Instantiate(actionButtonPrefab, actionButtonPanel);
                        btnObj.GetComponent<Image>().sprite = GetSpriteForAction(act.Type);
                        btnObj.GetComponent<Button>().onClick.AddListener(() =>
                            OnActionButtonClicked(act));
                    }
                }
            }

            moveTurn(RelativeSeat.SELF);
        }

        public void ProcessTsumoActions(JObject data)
        {
            UpdateLeftTilesByDelta(-1);

            ClearActionUI();

            isAfterTsumoAction = true;

            currentActionId = data["action_id"].ToObject<int>();
            remainingTime = data["left_time"].ToObject<float>();

            GameTile newTsumoTile = (GameTile)data["tile"].ToObject<int>();
            if (gameHandManager.GameHandPublic.HandSize < GameHand.FULL_HAND_SIZE)
            {
                StartCoroutine(gameHandManager.RunExclusive(gameHandManager.AddTsumo(newTsumoTile)));
            }
            if (timerText != null)
            {
                timerText.gameObject.SetActive(remainingTime > 0f);
                timerText.text = Mathf.FloorToInt(remainingTime).ToString();
            }

            var list = data["actions"].ToObject<List<GameAction>>();
            list.Sort();

            // Skip 버튼
            if (list.Count > 0)
            {
                isActionUIActive = true;
                var skip = Instantiate(actionButtonPrefab, actionButtonPanel);
                skip.GetComponent<Image>().sprite = skipButtonSprite;
                skip.GetComponent<Button>().onClick.AddListener(OnSkipButtonClickedAfterTsumo);
            }

            // CHII/KAN 추가선택지 로직
            var groups = list.GroupBy(a => a.Type).ToDictionary(g => g.Key, g => g.ToList());
            foreach (var kv in groups)
            {
                var type = kv.Key;
                var actionsOfType = kv.Value;

                if ((type == GameActionType.CHII || type == GameActionType.KAN)
                    && actionsOfType.Count > 1)
                {
                    var button = Instantiate(actionButtonPrefab, actionButtonPanel);
                    button.GetComponent<Image>().sprite = GetSpriteForAction(type);
                    button.GetComponent<Button>().onClick.AddListener(() =>
                        ShowAdditionalActionChoices(type, actionsOfType));
                }
                else
                {
                    foreach (var act in actionsOfType)
                    {
                        var btnObj = Instantiate(actionButtonPrefab, actionButtonPanel);
                        btnObj.GetComponent<Image>().sprite = GetSpriteForAction(act.Type);
                        btnObj.GetComponent<Button>().onClick.AddListener(() =>
                            OnActionButtonClicked(act));
                    }
                }
            }

            moveTurn(RelativeSeat.SELF);
        }

        private Sprite GetSpriteForAction(GameActionType type)
        {
            return type switch
            {
                GameActionType.CHII => chiiButtonSprite,
                GameActionType.PON => ponButtonSprite,
                GameActionType.KAN => kanButtonSprite,
                GameActionType.HU => huButtonSprite,
                GameActionType.FLOWER => flowerButtonSprite,
                _ => null
            };
        }
        private float CreateChoiceTileAt(GameTile tv, float startX, float blockHeight, Transform parent)
        {
            // 1) 스프라이트
            Sprite spr = Tile2DManager.Instance.get_sprite_by_name(tv.ToCustomString());
            if (spr == null) return startX;

            // 2) GameObject + Image
            var go = new GameObject($"ChoiceTile_{tv}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = spr;
            img.preserveAspect = true;
            img.raycastTarget = false;

            // 3) RectTransform 세팅 (pivot.x = 0)
            var rt = go.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            float ratio = spr.rect.width / spr.rect.height;
            rt.sizeDelta = new Vector2(blockHeight * ratio, blockHeight);

            // 4) 위치: startX 그대로
            rt.anchoredPosition = new Vector2(startX, 0f);

            // 5) 리턴: startX + width (반너비 더하지 않음)
            return startX + rt.sizeDelta.x;
        }

        private void ShowAdditionalActionChoices(GameActionType type, List<GameAction> choices)
        {
            // actionButtonPanel.gameObject.SetActive(false);
            choices.Sort((a, b) => ((int)a.Tile).CompareTo((int)b.Tile));

            // 1) 컨테이너 생성 + 반투명 검정 배경
            additionalChoicesContainer = new GameObject("AdditionalChoicesContainer",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            additionalChoicesContainer.transform.SetParent(actionButtonPanel.parent, false);
            var contRt = additionalChoicesContainer.GetComponent<RectTransform>();
            var bgImg = additionalChoicesContainer.GetComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.5f);  // 반투명 검정

            // 화면 아래쪽 30% 지점에 띄우기
            contRt.anchorMin = contRt.anchorMax = new Vector2(0.5f, 0.3f);
            contRt.pivot = new Vector2(0.5f, 0.5f);

            // 2) 크기 계산
            float blockH = 100f;
            float margin = 20f;
            int perCount = type == GameActionType.CHII ? 3 : 4;

            // 각 그룹 너비: 타일 3/4장 너비 + 좌우 마진
            var groupWS = choices.Select(act =>
            {
                var spr = Tile2DManager.Instance.get_sprite_by_name(act.Tile.ToCustomString());
                float ratio = spr.rect.width / spr.rect.height;
                return blockH * ratio * perCount + margin * 2;
            }).ToList();

            float totalW = groupWS.Sum()
                             + /*그룹 간격*/50f * (choices.Count - 1)
                             + margin * 2;
            float totalH = blockH + margin * 2;
            contRt.sizeDelta = new Vector2(totalW, totalH);

            // 3) Holder (HorizontalLayout) 세팅
            var holder = new GameObject("ChoicesHolder", typeof(RectTransform));
            holder.transform.SetParent(additionalChoicesContainer.transform, false);
            var hRt = holder.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0, 1);
            hRt.anchorMax = new Vector2(0, 1);
            hRt.pivot = new Vector2(0, 1);
            hRt.anchoredPosition = new Vector2(margin, -margin);
            hRt.sizeDelta = new Vector2(totalW - margin * 2, blockH);

            var hlg = holder.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30f;                  // 그룹 간 간격
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment = TextAnchor.UpperLeft;

            // 4) 각 선택지 그룹 버튼 생성
            for (int i = 0; i < choices.Count; i++)
            {
                var act = choices[i];
                float gW = groupWS[i];

                var choiceGO = new GameObject($"Choice_{i}",
                    typeof(RectTransform),
                    typeof(LayoutElement),
                    typeof(Button),
                    typeof(Image));
                choiceGO.transform.SetParent(holder.transform, false);

                // 크기
                var cRt = choiceGO.GetComponent<RectTransform>();
                cRt.sizeDelta = new Vector2(gW, blockH);
                var le = choiceGO.GetComponent<LayoutElement>();
                le.preferredWidth = gW;
                le.preferredHeight = blockH;

                // 투명하지만 Raycast 가능한 Image
                var bg = choiceGO.GetComponent<Image>();
                bg.color = new Color(0, 0, 0, 0f);
                bg.raycastTarget = true;

                // 버튼 클릭
                var btn = choiceGO.GetComponent<Button>();
                btn.targetGraphic = bg;
                btn.onClick.AddListener(() =>
                {
                    OnActionButtonClicked(act);
                });

                // 내부 타일 배치 (간격 0)
                float x = 0f;
                for (int j = 0; j < perCount; j++)
                {
                    GameTile tile = (type == GameActionType.CHII)
                        ? (GameTile)((int)act.Tile + j)
                        : act.Tile;

                    x = CreateChoiceTileAt(tile, x, blockH, choiceGO.transform);
                }
            }

            // 5) 뒤로가기 버튼 — 레이아웃 제외, 컨테이너 우상단 고정
            if (backButtonPrefab != null)
            {
                var back = Instantiate(backButtonPrefab, additionalChoicesContainer.transform);
                var bRt = back.GetComponent<RectTransform>();
                bRt.anchorMin = bRt.anchorMax = new Vector2(1, 1);
                bRt.pivot = new Vector2(1, 1);
                bRt.anchoredPosition = new Vector2(-margin, -margin);

                var ig = back.gameObject.AddComponent<LayoutElement>();
                ig.ignoreLayout = true;

                back.GetComponent<Button>().onClick.AddListener(() =>
                {
                    Destroy(additionalChoicesContainer);
                    actionButtonPanel.gameObject.SetActive(true);
                });
            }
        }

        private void OnActionButtonClicked(GameAction action)
        {
            Debug.Log($"액션 선택: {action.Type} / 타일: {action.Tile}");
            // TODO: 선택된 action_id와 action.Type, action.Tile 서버 전송
            SendSelectedAction(action: action);
            ClearActionUI();
        }

        private void SendSelectedAction(GameAction action)
        {
            var payload = new
            {
                action_type = action.Type,
                action_tile = action.Tile,
                action_id = currentActionId,
            };
            GameWS.Instance.SendGameEvent(action: GameWSActionType.RETURN_ACTION, payload: payload);
        }

        public void OnSkipButtonClicked()
        {
            ClearActionUI();
            Debug.Log("Skip 선택");
            GameAction SkipAction = new GameAction();
            SkipAction.Type = GameActionType.SKIP;
            SkipAction.Tile = GameTile.M1;
            SkipAction.SeatPriority = RelativeSeat.SELF;
            SendSelectedAction(action: SkipAction);
        }

        public void OnSkipButtonClickedAfterTsumo()
        {
            ClearActionButtons();
            Debug.Log("Skip 선택");
            GameAction SkipAction = new GameAction();
            SkipAction.Type = GameActionType.SKIP;
            SkipAction.Tile = GameTile.M1;
            SkipAction.SeatPriority = RelativeSeat.SELF;
            SendSelectedAction(action: SkipAction);
        }

        private void ClearActionButtons()
        {
            isActionUIActive = false;
            // 1) 액션 버튼들 제거
            foreach (Transform c in actionButtonPanel) Destroy(c.gameObject);
            if (additionalChoicesContainer != null)
            {
                Destroy(additionalChoicesContainer);
                additionalChoicesContainer = null;
            }
        }
        private void ClearActionUI()
        {
            isAfterTsumoAction = false;
            ClearActionButtons();

            // 2) 타이머 숨기기
            if (timerText != null)
                timerText.gameObject.SetActive(false);
        }

        /// <summary>
        /// 게임 시작 전/후 UI 전체 활성 토글
        /// </summary>
        private void SetUIActive(bool active)
        {
            // 기존 텍스트 UI 토글…
            if (leftTilesText != null) leftTilesText.gameObject.SetActive(active);
            if (currentRoundText != null) currentRoundText.gameObject.SetActive(active);

            foreach (var txt in new[] { windText_Self, windText_Shimo, windText_Toi, windText_Kami })
                if (txt != null) txt.gameObject.SetActive(active);

            foreach (var txt in new[] { scoreText_Self, scoreText_Shimo, scoreText_Toi, scoreText_Kami })
                if (txt != null) txt.gameObject.SetActive(active);

            // ───────────────────────────────────────────────
            // Profile UI 토글
            foreach (var img in profileImages) if (img != null) img.gameObject.SetActive(active);
            foreach (var frame in profileFrameImages) if (frame != null) frame.gameObject.SetActive(active);
            foreach (var txt in nicknameTexts) if (txt != null) txt.gameObject.SetActive(active);
            foreach (var img in flowerImages) if (img != null) img.gameObject.SetActive(active);
            foreach (var txt in flowerCountTexts) if (txt != null) txt.gameObject.SetActive(active);
        }
        private void UpdateCurrentRoundUI()
        {
            if (currentRoundText != null)
                currentRoundText.text = CurrentRound.ToString();
            else
                Debug.LogWarning("currentRoundText UI가 할당되지 않았습니다.");
        }

        public void ReloadData(JObject data)
        {
            Players = data["player_list"]
                .ToObject<List<Player>>();

            CurrentRound = (Round)data["current_round"].Value<int>();
            InitRoundSub(CurrentRound);

            currentTurnSeat = (RelativeSeat)data["current_turn_seat"].Value<int>();

            leftTiles = data["tiles_remaining"].Value<int>();
            UpdateLeftTiles(leftTiles);

            List<List<GameTile>> kawas = data["kawas"]
                .Select(arr => arr.ToObject<List<GameTile>>())
                .ToList();

            discardManager.ReloadAllDiscards(allTilesBySeat: kawas);

            List<List<CallBlockData>> CallBlocksList = data["call_blocks_list"]
                .Select(arr => arr.ToObject<List<CallBlockData>>())
                .ToList();

            List<GameTile> RawHand = data["hand"]
                .Select(t => (GameTile)t.Value<int>())
                .ToList();
            List<CallBlockData> RawCallBockList = CallBlocksList[(int)MySeat];


            var tsumoToken = data["tsumo_tile"];
            GameTile? TsumoTile = (tsumoToken.Type == JTokenType.Null)
                ? (GameTile?)null
                : (GameTile)tsumoToken.Value<int>();

            gameHandManager.ReloadInitHand(rawTiles: RawHand, rawCallBlocks: RawCallBockList, rawTsumoTile: TsumoTile);

            List<int> HandsCount = data["hands_count"]
                .ToObject<List<int>>();

            List<int> TsumoTilesCount = data["tsumo_tiles_count"]
                .ToObject<List<int>>();

            for (int seat = 0; seat < MAX_PLAYERS; ++seat)
            {
                if ((RelativeSeat)seat == RelativeSeat.SELF) continue;
                AbsoluteSeat absoluteSeat = RelativeSeatExtensions.ToAbsoluteSeat(rel: (RelativeSeat)seat, mySeat: MySeat);
                callBlockFields[seat].ReloadCallBlockListImmediate(CallBlocksList[(int)absoluteSeat]);
                playersHand3DFields[seat].ReloadInitHand(handCount: HandsCount[(int)absoluteSeat], includeTsumo: TsumoTilesCount[(int)absoluteSeat] == 1);
            }

            List<int> FlowersCount = data["flowers_count"]
                .ToObject<List<int>>();
            InitializeFlowerUI();
            for (int seat = 0; seat < MAX_PLAYERS; ++seat)
            {
                AbsoluteSeat absoluteSeat = RelativeSeatExtensions.ToAbsoluteSeat(rel: (RelativeSeat)seat, mySeat: MySeat);
                SetFlowerCount(rel: (RelativeSeat)seat, FlowersCount[(int)absoluteSeat]);
            }

            moveTurn(currentTurnSeat);
            currentActionId = data["action_id"].Value<int>();

            // remaining time 처리
            List<List<GameAction>> ActionList = data["action_choices_list"]
                .Select(arr => arr.ToObject<List<GameAction>>())
                .ToList();
            remainingTime = data["remaining_time"].Value<float>();

            if (currentTurnSeat == RelativeSeat.SELF)
            {
                ReloadTsumoActions(ActionList[(int)MySeat]);
            }
            else
            {
                if (ActionList[(int)MySeat].Count > 0)
                {
                    ReloadDiscardActions(ActionList[(int)MySeat]);
                }
            }
            Debug.Log($"[GameManager] ReloadData 완료 - 남은 시간: {remainingTime:F2}s");
        }


        private void InitRoundSub(Round round)
        {
            leftTiles = MAX_TILES - (GameHand.FULL_HAND_SIZE - 1) * MAX_PLAYERS;
            IsFlowerConfirming = false;
            isActionUIActive = false;
            isAfterTsumoAction = false;
            CanClick = false;
            SetUIActive(true);
            ClearActionUI();
            discardManager.InitRound();
            UpdateLeftTiles(leftTiles);
            foreach (var CBField in callBlockFields)
            {
                CBField.InitializeCallBlockField();
            }
            gameHandManager.clear();

            foreach (var hand3DField in playersHand3DFields)
            {
                hand3DField.clear();
            }

            CurrentRound = round;

            ResetAllBlinkTurnEffects();

            UpdateCurrentRoundUI();
            InitSeatIndexMapping();

            UpdateSeatLabels();
            UpdateScoreText();
            InitializeProfileUI();
            InitializeFlowerUI();
        }

        private void InitRound()
        {
            if (isGameStarted)
            {
                if (CurrentRound.NextRound() != Round.END)
                    CurrentRound = CurrentRound.NextRound();
            }
            else
            {
                CurrentRound = Round.E1;
                isGameStarted = true;
            }
            InitRoundSub(CurrentRound);
        }


        private void InitializeFlowerUI()
        {
            flowerCountMap.Clear();
            foreach (RelativeSeat rel in Enum.GetValues(typeof(RelativeSeat)))
                flowerCountMap[rel] = 0;
            UpdateFlowerCountText();
        }

        public void SetFlowerCount(RelativeSeat rel, int count)
        {
            flowerCountMap[rel] = count;
            UpdateFlowerCountText();
        }

        public void UpdateFlowerCountText()
        {
            foreach (RelativeSeat rel in Enum.GetValues(typeof(RelativeSeat)))
            {
                int index = (int)rel;

                // 유효성 검사
                if (flowerImages.Length <= index || flowerCountTexts.Length <= index)
                    continue;

                var image = flowerImages[index];
                var text = flowerCountTexts[index];

                if (!flowerCountMap.TryGetValue(rel, out int count))
                    count = 0;

                if (count == 0)
                {
                    image?.gameObject.SetActive(false);
                    text?.gameObject.SetActive(false);
                    image.sprite = FlowerIcon_White;
                    text.text = "X0";
                }
                else
                {
                    image?.gameObject.SetActive(true);
                    text?.gameObject.SetActive(true);

                    if (count >= 1 && count <= 3)
                        image.sprite = FlowerIcon_White;
                    else if (count >= 4 && count <= 6)
                        image.sprite = FlowerIcon_Yellow;
                    else
                        image.sprite = FlowerIcon_Red;

                    text.text = $"X{count}";
                }
            }
        }

        private void setRelativeSeatFlowerUIActive(bool isActive, RelativeSeat seat)
        {
            int index = (int)seat;

            // 유효성 검사
            if (flowerImages.Length <= index || flowerCountTexts.Length <= index)
                return;

            var image = flowerImages[index];
            var text = flowerCountTexts[index];

            image?.gameObject.SetActive(isActive);
            text?.gameObject.SetActive(isActive);
        }

        private void InitializeProfileUI()
        {
            for (int i = 0; i < 4; i++)
            {
                var rel = (RelativeSeat)i;
                var abs = rel.ToAbsoluteSeat(MySeat);

                if (!seatToPlayerIndex.TryGetValue(abs, out int idx) || idx < 0 || idx >= Players.Count)
                    continue;

                var player = Players[idx];

                // 1) 닉네임
                if (nicknameTexts[i] != null)
                    nicknameTexts[i].text = player.Nickname;

                // 2) 프로필 이미지
                if (profileImages[i] != null)
                    profileImages[i].sprite = GetProfileImageSprite(player.Uid); // 필요시 구현

                // 3) 프로필 프레임
                if (profileFrameImages[i] != null)
                {
                    profileFrameImages[i].sprite = GetFrameSprite(player.Uid); // 필요시 구현
                    profileFrameImages[i].gameObject.SetActive(true);
                }

                // 4) 꽃 UI 초기화 (이미 InitializeFlowerUI 에서 처리됨)
            }
        }

        private Sprite GetProfileImageSprite(string uid)
        {
            return defaultProfileImageSprite;
        }

        // 플레이어별 프레임 스프라이트 반환 (예시: 모두 동일 프레임 사용하거나,
        // Player 데이터에 frame 정보가 있으면 거기서 가져오면 됩니다)
        private Sprite GetFrameSprite(string uid)
        {
            // TODO: uid별로 다른 프레임을 쓰려면 이곳에 로직 추가
            // 지금은 Inspector에서 미리 할당한 기본 프레임을 반환하도록 하겠습니다.
            return defaultFrameSprite;
        }

        // Deal 1~16 의 (idx0,1,2,3) → 좌석 순서
        private static readonly AbsoluteSeat[][] DEAL_TABLE =
        {
        new[]{ AbsoluteSeat.EAST,  AbsoluteSeat.SOUTH, AbsoluteSeat.WEST,  AbsoluteSeat.NORTH }, // 1
        new[]{ AbsoluteSeat.NORTH, AbsoluteSeat.EAST,  AbsoluteSeat.SOUTH, AbsoluteSeat.WEST  }, // 2
        new[]{ AbsoluteSeat.WEST,  AbsoluteSeat.NORTH, AbsoluteSeat.EAST,  AbsoluteSeat.SOUTH }, // 3
        new[]{ AbsoluteSeat.SOUTH, AbsoluteSeat.WEST,  AbsoluteSeat.NORTH, AbsoluteSeat.EAST  }, // 4
        new[]{ AbsoluteSeat.SOUTH, AbsoluteSeat.EAST,  AbsoluteSeat.NORTH, AbsoluteSeat.WEST  }, // 5
        new[]{ AbsoluteSeat.EAST,  AbsoluteSeat.NORTH, AbsoluteSeat.WEST,  AbsoluteSeat.SOUTH }, // 6
        new[]{ AbsoluteSeat.NORTH, AbsoluteSeat.WEST,  AbsoluteSeat.SOUTH, AbsoluteSeat.EAST  }, // 7
        new[]{ AbsoluteSeat.WEST,  AbsoluteSeat.SOUTH, AbsoluteSeat.EAST,  AbsoluteSeat.NORTH }, // 8
        new[]{ AbsoluteSeat.NORTH, AbsoluteSeat.WEST,  AbsoluteSeat.EAST,  AbsoluteSeat.SOUTH }, // 9
        new[]{ AbsoluteSeat.WEST,  AbsoluteSeat.SOUTH, AbsoluteSeat.NORTH, AbsoluteSeat.EAST  }, // 10
        new[]{ AbsoluteSeat.SOUTH, AbsoluteSeat.EAST,  AbsoluteSeat.WEST,  AbsoluteSeat.NORTH }, // 11
        new[]{ AbsoluteSeat.EAST,  AbsoluteSeat.NORTH, AbsoluteSeat.SOUTH, AbsoluteSeat.WEST  }, // 12
        new[]{ AbsoluteSeat.WEST,  AbsoluteSeat.NORTH, AbsoluteSeat.SOUTH, AbsoluteSeat.EAST  }, // 13
        new[]{ AbsoluteSeat.SOUTH, AbsoluteSeat.WEST,  AbsoluteSeat.EAST,  AbsoluteSeat.NORTH }, // 14
        new[]{ AbsoluteSeat.EAST,  AbsoluteSeat.SOUTH, AbsoluteSeat.NORTH, AbsoluteSeat.WEST  }, // 15
        new[]{ AbsoluteSeat.NORTH, AbsoluteSeat.EAST,  AbsoluteSeat.WEST,  AbsoluteSeat.SOUTH }, // 16
    };

        /// <summary>
        /// deal(1~16)에 대한 좌석-인덱스 매핑을 얻는다.
        /// </summary>
        public static void GetSeatMappings(int deal,
            out Dictionary<AbsoluteSeat, int> seatToPlayer,
            out Dictionary<int, AbsoluteSeat> playerToSeat)
        {

            AbsoluteSeat[] order = DEAL_TABLE[deal];

            seatToPlayer = new Dictionary<AbsoluteSeat, int>(4);
            playerToSeat = new Dictionary<int, AbsoluteSeat>(4);
            for (int i = 0; i < 4; i++)
            {
                seatToPlayer[order[i]] = i;
                playerToSeat[i] = order[i];
            }
        }


        /* 사용 예
        Dictionary<AbsoluteSeat,int> seat2idx;
        Dictionary<int,AbsoluteSeat> idx2seat;
        DealSeatMapper.GetSeatMappings(1, out seat2idx, out idx2seat);
        // seat2idx[AbsoluteSeat.EAST] == 0,  idx2seat[2] == AbsoluteSeat.WEST
        */
        public void InitSeatIndexMapping()
        {

            GetSeatMappings((int)CurrentRound, out seatToPlayerIndex, out playerIndexToSeat);

            // ➍ 내 절대 좌석 & 현재 턴 좌석
            MySeat = playerIndexToSeat[playerUidToIndex[PlayerDataManager.Instance.Uid]];
            CurrentTurnSeat = RelativeSeatExtensions.CreateFromAbsoluteSeats(
                                  currentSeat: MySeat, targetSeat: AbsoluteSeat.EAST);
        }


        public void UpdateLeftTilesByDelta(int delta)
        {
            UpdateLeftTiles(leftTiles + delta);
        }
        public void UpdateLeftTiles(int newValue)
        {
            leftTiles = newValue;
            if (leftTilesText != null)
            {
                leftTilesText.text = leftTiles.ToString();
            }
            else
            {
                Debug.LogWarning("leftTilesText UI가 할당되지 않았습니다.");
            }
        }

        public void UpdateScoreText()
        {
            var scoreMap = new Dictionary<RelativeSeat, TextMeshProUGUI>
    {
        { RelativeSeat.SELF,  scoreText_Self  },
        { RelativeSeat.SHIMO, scoreText_Shimo },
        { RelativeSeat.TOI,   scoreText_Toi   },
        { RelativeSeat.KAMI,  scoreText_Kami  },
    };

            Debug.Log("[UpdateScoreText] 시작 — MySeat: " + MySeat);

            foreach (var kv in scoreMap)
            {
                var rel = kv.Key;
                var txt = kv.Value;
                if (txt == null)
                {
                    Debug.LogWarning($"[UpdateScoreText] 텍스트 컴포넌트 누락: {rel}");
                    continue;
                }

                // Relative → Absolute → Player index
                var absSeat = rel.ToAbsoluteSeat(MySeat);
                Debug.Log($"[UpdateScoreText] rel: {rel} → abs: {absSeat}");

                if (!seatToPlayerIndex.TryGetValue(absSeat, out int idx))
                {
                    Debug.LogWarning($"[UpdateScoreText] seatToPlayerIndex에 없음: {absSeat}");
                    continue;
                }
                if (idx < 0 || idx >= Players.Count)
                {
                    Debug.LogWarning($"[UpdateScoreText] 잘못된 플레이어 인덱스: {idx} (Players.Count={Players.Count})");
                    continue;
                }

                int score = Players[idx].Score;
                Debug.Log($"[UpdateScoreText] Players[{idx}].Score = {score}");

                if (score < 0)
                {
                    txt.text = score.ToString();
                    txt.color = negativeScoreColor;
                    Debug.Log($"[UpdateScoreText] 음수 분기: text='{txt.text}', color={negativeScoreColor}");
                }
                else if (score > 0)
                {
                    txt.text = "+" + score.ToString();
                    txt.color = positiveScoreColor;
                    Debug.Log($"[UpdateScoreText] 양수 분기: text='{txt.text}', color={positiveScoreColor}");
                }
                else // score == 0
                {
                    txt.text = "+0";
                    txt.color = zeroScoreColor;
                    Debug.Log($"[UpdateScoreText] 0점 분기: text='{txt.text}', color={zeroScoreColor}");
                }
            }

            Debug.Log("[UpdateScoreText] 완료");
        }


        public void UpdateSeatLabels()
        {
            var windMap = new Dictionary<RelativeSeat, TextMeshProUGUI>
            {
                { RelativeSeat.SELF,  windText_Self  },
                { RelativeSeat.SHIMO, windText_Shimo },
                { RelativeSeat.TOI,   windText_Toi   },
                { RelativeSeat.KAMI,  windText_Kami  },
            };

            AbsoluteSeat seat = MySeat;
            for (RelativeSeat rel = RelativeSeat.SELF; ; rel = rel.NextSeat())
            {
                if (windMap.TryGetValue(rel, out var txt) && txt != null)
                {
                    string label = seat.ToString().Substring(0, 1);
                    txt.text = label;
                    txt.color = (label == "E") ? eastWindColor : otherWindColor;
                }
                seat = seat.NextSeat();
                if (rel == RelativeSeat.KAMI) break;
            }
        }

        /// <summary>
        /// 서버로부터 전달된 플레이어 목록으로 게임을 초기화합니다.
        /// Players 리스트 세팅과, UID→인덱스 매핑을 만듭니다.
        /// </summary>
        public void InitGame(List<Player> players)
        {
            // 1) Deep copy: 새로운 Player 인스턴스 리스트 생성
            Players = players
                .Select(p => new Player(p.Uid, p.Nickname, p.Index, p.Score))
                .ToList();

            // 2) UID → index 매핑 초기화
            playerUidToIndex = Players.ToDictionary(p => p.Uid, p => p.Index);

            Debug.Log($"GameManager: Game initialized with {Players.Count} players.");
        }

        public IEnumerator InitHandCoroutine(List<GameTile> tiles, GameTile? tsumoTile)
        {
            isInitHandDone = false;
            yield return StartCoroutine(InitHandFromMessage(tiles, tsumoTile));
            isInitHandDone = true;
            yield return new WaitForSeconds(0.5f);
            Debug.Log("[GameMessageMediator] InitHand complete. Processing any queued flower replacement messages.");

            foreach (var msg in pendingFlowerReplacement)
                ProcessInitFlowerReplacement(msg);
            pendingFlowerReplacement.Clear();
        }

        public void ProcessInitFlowerReplacement(GameWSMessage message)
        {
            List<GameTile> newTiles = null;
            List<GameTile> appliedFlowers = null;
            List<int> flowerCounts = null;

            if (message.Data.TryGetValue("new_tiles", out JToken tilesToken))
            {
                var newTilesInts = tilesToken.ToObject<List<int>>();
                newTiles = newTilesInts.Select(i => (GameTile)i).ToList();
            }

            if (message.Data.TryGetValue("applied_flowers", out JToken appliedFlowersToken))
            {
                appliedFlowers = appliedFlowersToken.ToObject<List<GameTile>>();
            }

            if (message.Data.TryGetValue("flower_count", out JToken countToken))
            {
                flowerCounts = countToken.ToObject<List<int>>();
            }

            if (newTiles != null && appliedFlowers != null && flowerCounts != null)
            {
                Debug.Log("[GameMessageMediator] Starting flower replacement coroutine.");
                StartCoroutine(gameHandManager.RunExclusive(StartFlowerReplacement(newTiles, appliedFlowers, flowerCounts)));
            }
            else
            {
                Debug.LogWarning("[GameMessageMediator] One or more flower replacement parameters were missing.");
            }
        }


        /// <summary>
        /// INIT_EVENT 메시지를 통해 받은 초기 손패 데이터를 SELF의 손패와
        /// 플레이어들의 3D 손패 필드에 반영합니다.
        /// </summary>
        /// <param name="initTiles">자신의 초기 손패 타일 데이터 리스트</param>
        /// <param name="tsumoTile">서버에서 받은 tsumotile (없으면 null)</param>
        public IEnumerator InitHandFromMessage(List<GameTile> initTiles, GameTile? tsumoTile)
        {
            InitRound();
            CanClick = false;
            gameHandManager.IsAnimating = true;
            Debug.Log("GameManager: Initializing hand with received data for SELF.");

            // 1) 2D 핸드(UI) 초기화
            if (gameHandManager != null)
            {
                // 기본 init (initTiles 수만큼 4장씩 떨어뜨림)
                yield return gameHandManager.RunExclusive(gameHandManager.InitHand(initTiles, tsumoTile));

            }
            else
            {
                Debug.LogWarning("GameManager: GameHandManager 인스턴스가 없습니다.");
            }

            // 2) 3D 필드(상대방 포함) 초기화
            if (playersHand3DFields == null || playersHand3DFields.Length < MAX_PLAYERS)
            {
                Debug.LogError("playersHand3DFields 배열이 4개로 할당되어 있지 않습니다.");
                yield break;
            }
            for (int i = 0; i < playersHand3DFields.Length; i++)
            {
                Hand3DField hand3DField = playersHand3DFields[i];
                if (hand3DField == null)
                {
                    Debug.LogWarning($"Hand3DField가 배열의 index {i}에서 할당되지 않았습니다.");
                    continue;
                }

                // SELF(자기)는 이미 2D 핸드로 처리했으니 건너뛰기
                if (i == (int)RelativeSeat.SELF)
                    continue;

                // RelativeSeat → AbsoluteSeat 변환
                RelativeSeat rel = (RelativeSeat)i;
                AbsoluteSeat abs = rel.ToAbsoluteSeat(MySeat);

                // EAST면 tsumo 포함(14장), 아니면 13장만
                bool includeTsumo = (abs == AbsoluteSeat.EAST);
                hand3DField.InitHand(includeTsumo);
            }

        }

        public IEnumerator StartFlowerReplacement(List<GameTile> newTiles, List<GameTile> appliedFlowers, List<int> flowerCounts)
        {
            yield return StartCoroutine(FlowerReplacementCoroutine(newTiles, appliedFlowers, flowerCounts));
            gameHandManager.IsAnimating = false;
            CanClick = false;
        }

        private IEnumerator FlowerReplacementCoroutine(List<GameTile> newTiles, List<GameTile> appliedFlowers, List<int> flowerCounts)
        {
            // GameHandManager의 InitHand 완료 여부 체크
            while (!gameHandManager.IsInitHandComplete)
            {
                yield return null;
            }
            if (MySeat != AbsoluteSeat.EAST)
            {
                yield return new WaitForSeconds(0.4f);
            }
            Debug.Log("FlowerReplacementCoroutine: InitHand 완료 확인. 꽃 교체 이벤트 시작.");
            yield return new WaitForSeconds(0.7f);

            // "Main 2D Canvas" 오브젝트를 찾아 그 자식으로 prefab을 Instantiate합니다.
            GameObject mainCanvasObject = GameObject.Find("Main 2D Canvas");
            Transform canvasTransform = mainCanvasObject != null ? mainCanvasObject.transform : transform;

            // 0) FLOWER PHASE 연출: fade in 해서 유지
            GameObject flowerEffect = null;
            Image flowerPhaseImage = null;
            if (flowerPhaseEffectPrefab != null)
            {
                flowerEffect = Instantiate(flowerPhaseEffectPrefab, canvasTransform);
                flowerPhaseImage = flowerEffect.GetComponent<Image>();
                if (flowerPhaseImage != null)
                {
                    // fade in (0.2초 동안 서서히 나타남)
                    yield return StartCoroutine(FadeIn(flowerPhaseImage, 0.2f));
                    // 이 후 flowerPhaseImage는 화면에 완전히 나타난 상태로 유지됩니다.
                }
            }
            else
            {
                Debug.LogWarning("flowerPhaseEffectPrefab이 할당되지 않았습니다.");
            }

            // 1) 좌석 순서: EAST, SOUTH, WEST, NORTH
            AbsoluteSeat[] seats = new AbsoluteSeat[] { AbsoluteSeat.EAST, AbsoluteSeat.SOUTH, AbsoluteSeat.WEST, AbsoluteSeat.NORTH };

            foreach (var absoluteSeat in seats)
            {
                int count = flowerCounts[(int)absoluteSeat];
                Debug.Log($"[FlowerReplacement] {absoluteSeat} 좌석 꽃 개수: {count}");

                RelativeSeat relativeSeat = RelativeSeatExtensions.CreateFromAbsoluteSeats(MySeat, absoluteSeat);
                if (relativeSeat == RelativeSeat.SELF)
                {
                    int currentFlowerCount = 0; // 초기 꽃 개수 (필요 시 기존 값 반영)
                    for (int i = 0; i < count; i++)
                    {
                        int previousCount = currentFlowerCount;
                        currentFlowerCount++;

                        bool operationDone = false;
                        bool animateDone = false;

                        // 동시에 꽃 카운트 애니메이션 실행
                        StartCoroutine(AnimateFlowerCount(relativeSeat, previousCount, currentFlowerCount, () => { animateDone = true; }));
                        // 두 작업(ApplyFlower와 AddInitFlowerTsumo)을 순차 실행하는 코루틴 실행
                        StartCoroutine(ProcessFlowerOperation(i, newTiles, appliedFlowers, () => { operationDone = true; }));

                        leftTiles--;
                        UpdateLeftTiles(leftTiles);
                        // 두 작업 모두 완료될 때까지 기다림
                        yield return new WaitUntil(() => operationDone && animateDone);
                    }
                    SetFlowerCount(relativeSeat, currentFlowerCount);
                }
                else
                {
                    // 상대의 경우: Hand3DField를 이용해 요청
                    Hand3DField handField = playersHand3DFields[(int)relativeSeat];
                    int currentFlowerCount = 0; // 상대 좌석의 초기 꽃 개수
                    for (int i = 0; i < count; i++)
                    {
                        int previousCount = currentFlowerCount;
                        currentFlowerCount++;

                        bool operationDone = false;
                        bool animateDone = false;

                        // 동시에 꽃 카운트 애니메이션 실행
                        StartCoroutine(AnimateFlowerCount(relativeSeat, previousCount, currentFlowerCount, () => { animateDone = true; }));
                        // Hand3DField의 RequestDiscardRandom과 RequestInitFlowerTsumo를 순차 실행하는 코루틴
                        StartCoroutine(ProcessOpponentFlowerOperation(handField, () => { operationDone = true; }));
                        leftTiles--;
                        UpdateLeftTiles(leftTiles);
                        yield return new WaitUntil(() => operationDone && animateDone);
                    }
                    SetFlowerCount(relativeSeat, currentFlowerCount);
                }
                yield return new WaitForSeconds(0.3f);
            }

            // 2) 모든 꽃 교체 완료 후, ROUND START 연출 직전에 FLOWER PHASE fade out 처리
            if (flowerPhaseImage != null)
            {
                yield return StartCoroutine(FadeOut(flowerPhaseImage, 0.2f));
                Destroy(flowerEffect);
            }
            yield return new WaitForSeconds(1f);
            // 3) 전체 화패 교체 이벤트 종료 후 ROUND START 연출
            if (roundStartEffectPrefab != null)
            {
                GameObject roundStartEffect = Instantiate(roundStartEffectPrefab, canvasTransform);
                Image roundStartImage = roundStartEffect.GetComponent<Image>();
                if (roundStartImage != null)
                {
                    yield return StartCoroutine(FadeInAndOut(roundStartImage, 0.2f, 0.7f));
                }
                Destroy(roundStartEffect);
            }
            else
            {
                Debug.LogWarning("roundStartEffectPrefab이 할당되지 않았습니다.");
            }
            if (GameWS.Instance != null)
            {
                GameWS.Instance.SendGameEvent(GameWSActionType.GAME_EVENT, new
                {
                    event_type = (int)GameEventType.INIT_FLOWER_OK,
                    data = new Dictionary<string, object>()
                });
            }
            Debug.Log("[FlowerReplacement] 꽃 교체 이벤트 완료.");
            yield break;
        }


        // SELF인 경우 두 작업(ApplyFlower와 AddInitFlowerTsumo)을 순차적으로 실행합니다.
        private IEnumerator ProcessFlowerOperation(int index, List<GameTile> newTiles, List<GameTile> appliedFlowers, Action onComplete)
        {
            yield return gameHandManager.RunExclusive(gameHandManager.ApplyFlower(appliedFlowers[index]));
            yield return gameHandManager.RunExclusive(gameHandManager.AddInitFlowerTsumo(newTiles[index]));
            onComplete?.Invoke();
        }

        // 상대의 경우, Hand3DField의 RequestDiscardRandom()과 RequestInitFlowerTsumo()를 순차적으로 실행합니다.
        private IEnumerator ProcessOpponentFlowerOperation(Hand3DField handField, Action onComplete)
        {
            yield return StartCoroutine(handField.RequestDiscardRandom());
            yield return StartCoroutine(handField.RequestInitFlowerTsumo());
            onComplete?.Invoke();
        }
        private IEnumerator AnimateFlowerCount(RelativeSeat rel, int fromValue, int toValue, Action onComplete)
        {
            float duration = 0.1f;
            float elapsed = 0f;
            // 해당 좌석의 꽃 카운트 텍스트를 가져옵니다.
            TextMeshProUGUI flowerCountText = flowerCountTexts[(int)rel];
            // 텍스트와 이미지가 동일한 부모 오브젝트에 있다고 가정합니다.
            Transform container = flowerCountText.transform.parent;

            // 부모 컨테이너의 원래 스케일을 저장합니다.
            Vector3 originalScale = container.localScale;
            // popScale : 중간에 도달할 최대 확대 비율 (예: 1.3배)
            float popScale = 1.3f;

            // UI 활성화 (필요 시)
            setRelativeSeatFlowerUIActive(isActive: true, seat: rel);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // 숫자 애니메이션: SmoothStep을 적용하여 fromValue에서 toValue로 보간
                float eased = Mathf.SmoothStep(0f, 1f, t);
                int displayValue = fromValue + Mathf.RoundToInt(eased * (toValue - fromValue));
                flowerCountText.text = "X" + displayValue.ToString();

                // 팝 효과: 4*t*(1-t)는 t=0과 t=1에서 0, t=0.5에서 최대값을 가지도록 함.
                float scaleFactor = 1 + (popScale - 1) * (4 * t * (1 - t));
                // 부모 컨테이너 스케일을 변경하면, 자식인 텍스트와 이미지 모두 영향을 받습니다.
                container.localScale = originalScale * scaleFactor;
                yield return null;
            }
            // 최종 값 적용 및 스케일 복귀
            flowerCountText.text = "X" + toValue.ToString();
            container.localScale = originalScale;
            onComplete?.Invoke();
        }

        // 지정한 Image 컴포넌트가 fade in 효과로 나타나도록 처리 (fadeDuration 동안)
        private IEnumerator FadeIn(Image img, float fadeDuration)
        {
            Color origColor = img.color;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                img.color = new Color(origColor.r, origColor.g, origColor.b, t);
                yield return null;
            }
            img.color = new Color(origColor.r, origColor.g, origColor.b, 1f);
        }

        // 지정한 Image 컴포넌트가 fade out 효과로 사라지도록 처리 (fadeDuration 동안)
        private IEnumerator FadeOut(Image img, float fadeDuration)
        {
            Color origColor = img.color;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                img.color = new Color(origColor.r, origColor.g, origColor.b, 1 - t);
                yield return null;
            }
            img.color = new Color(origColor.r, origColor.g, origColor.b, 0f);
        }


        /// <summary>
        /// Image 컴포넌트에 대해 FadeIn 후 일정 시간 유지, FadeOut 애니메이션을 수행합니다.
        /// </summary>
        private IEnumerator FadeInAndOut(Image img, float fadeDuration, float displayDuration)
        {
            Color origColor = img.color;
            // Fade In
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                img.color = new Color(origColor.r, origColor.g, origColor.b, t);
                yield return null;
            }
            yield return new WaitForSeconds(displayDuration);
            // Fade Out
            elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                img.color = new Color(origColor.r, origColor.g, origColor.b, 1 - t);
                yield return null;
            }
        }

        public void EndScorePopup()
        {
            if (EndScorePopupPrefab == null)
            {
                Debug.LogError("ScorePopupPrefab이 할당되지 않았습니다.");
                return;
            }
            // 씬에 Canvas 포함 프리팹을 그대로 생성
            GameObject popupGO = Instantiate(EndScorePopupPrefab);
            var mgr = popupGO.GetComponentInChildren<EndScorePopupManager>();
            if (mgr == null)
            {
                Debug.LogError("EndScorePopupManager를 찾을 수 없습니다.");
                return;
            }
            StartCoroutine(mgr.ShowScores(Players));
        }

        public IEnumerator ProcessDraw(
            List<List<GameTile>> anKanInfos
        )
        {
            isInitHandDone = false;
            yield return StartCoroutine(cameraResultAnimator.PlayResultAnimation());
            yield return new WaitForSeconds(3f);
            cameraResultAnimator.ResetCameraState();
            if (GameWS.Instance != null)
            {
                GameWS.Instance.SendGameEvent(GameWSActionType.GAME_EVENT, new
                {
                    event_type = (int)GameEventType.NEXT_ROUND_CONFIRM,
                    data = new Dictionary<string, object>()
                });
            }
            ResetAllBlinkTurnEffects();
            // if (CurrentRound == Round.N4)
            // {
            //     EndScorePopup();
            // }
        }

        public IEnumerator ProcessHuHand(
            List<GameTile> handTiles,
            List<CallBlockData> callBlocks,
            ScoreResult scoreResult,
            AbsoluteSeat winPlayerSeat,
            AbsoluteSeat currentPlayerSeat,
            int flowerCount,
            GameTile? tsumoTile,
            List<List<GameTile>> anKanInfos,
            GameTile winningTile
        )
        {
            ActionAudioManager.Instance?.EnqueueHuSound();


            isInitHandDone = false;
            ClearActionUI();
            CanClick = false;
            gameHandManager.IsAnimating = true;
            handTiles.Sort();
            int singleScore = scoreResult.total_score + flowerCount;
            int total_score = (winPlayerSeat == currentPlayerSeat ? singleScore * 3 : singleScore) + 24;
            scoreResult.yaku_score_list.Add(new YakuScore(yid: (int)Yaku.FlowerPoint, score: flowerCount));
            WinningScoreData wsd = new WinningScoreData(handTiles, callBlocks, singleScore, total_score, scoreResult.yaku_score_list, winPlayerSeat, flowerCount, winningTile);


            // 3D 핸드 필드 새로 생성: 승리한 플레이어의 핸드 필드를 클리어하고 실제 타일로 재구성
            // (tsumoTile과 동일한 타일 한 개는 표시하지 않고, 마지막에 tsumoTile을 추가하여 extra gap이 적용되도록)
            Hand3DField targetHandField = playersHand3DFields[(int)RelativeSeatExtensions.CreateFromAbsoluteSeats(currentSeat: MySeat, targetSeat: winPlayerSeat)];
            targetHandField.clear();
            targetHandField.MakeRealHand(tsumoTile, handTiles);
            // "Main 2D Canvas" 이름의 GameObject 찾기
            GameObject canvas = GameObject.Find("Main 2D Canvas");
            if (canvas == null)
            {
                Debug.LogWarning("Main 2D Canvas를 찾을 수 없습니다.");
                yield break;
            }
            canvas.SetActive(false);
            Debug.Log("Canvas 비활성화 완료.");
            yield return StartCoroutine(cameraResultAnimator.PlayResultAnimation());
            yield return StartCoroutine(targetHandField.AnimateAllTilesRotationDomino(baseDuration: 0.4f, handScore: singleScore));
            yield return new WaitForSeconds(5f);
            ScorePopupManager.Instance.ShowWinningPopup(wsd);
            Debug.Log("processed hu hand.");
            yield return new WaitForSeconds(5f);
            cameraResultAnimator.ResetCameraState();
            ScorePopupManager.Instance.DeleteWinningPopup();
            canvas.SetActive(true);
            Debug.Log("Canvas 활성화 완료.");

            UpdateScoreText();
            ResetAllBlinkTurnEffects();
            if (GameWS.Instance != null)
            {
                GameWS.Instance.SendGameEvent(GameWSActionType.GAME_EVENT, new
                {
                    event_type = (int)GameEventType.NEXT_ROUND_CONFIRM,
                    data = new Dictionary<string, object>()
                });
            }
            // if (CurrentRound == Round.N4)
            // {
            //     EndScorePopup();
            // }
        }

    }
}