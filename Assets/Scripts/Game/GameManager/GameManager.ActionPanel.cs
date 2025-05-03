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
using DG.Tweening;


namespace MCRGame.Game
{
    public partial class GameManager
    {
/*──────────────────────────────────────────────────*/
/*           🎯 ACTION  PANEL (버튼 & 선택지)           */
/*──────────────────────────────────────────────────*/
#region 🎯 ACTION PANEL

    /* -------- ① Public Reload API -------- */
    #region ▶ Reload & Build Buttons API
        /*  (원본 함수 내용은 다른 partial 파일에 이미 존재)  
            void ReloadTsumoActions(List<GameAction> list)
            void ReloadDiscardActions(List<GameAction> list)
        */
    #endregion


    /* -------- ② UI Helper -------- */
    #region ▶ UI Helpers (Sprite / Tile 이미지 등)

        private Sprite GetSpriteForAction(GameActionType type)
        {
            return type switch
            {
                GameActionType.CHII   => chiiButtonSprite,
                GameActionType.PON    => ponButtonSprite,
                GameActionType.KAN    => kanButtonSprite,
                GameActionType.HU     => huButtonSprite,
                GameActionType.FLOWER => flowerButtonSprite,
                _                     => null
            };
        }

        private float CreateChoiceTileAt(GameTile tv, float startX, float blockHeight, Transform parent)
        {
            // 1) 스프라이트
            Sprite spr = Tile2DManager.Instance.get_sprite_by_name(tv.ToCustomString());
            if (spr == null) return startX;

            // 2) GameObject + Image
            var go  = new GameObject($"ChoiceTile_{tv}", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite         = spr;
            img.preserveAspect = true;
            img.raycastTarget  = false;

            // 3) RectTransform
            var rt   = go.GetComponent<RectTransform>();
            rt.pivot = new Vector2(0f, 0.5f);
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
            float ratio = spr.rect.width / spr.rect.height;
            rt.sizeDelta        = new Vector2(blockHeight * ratio, blockHeight);

            // 4) 위치 지정
            rt.anchoredPosition = new Vector2(startX, 0f);

            // 5) 다음 X 반환
            return startX + rt.sizeDelta.x;
        }

    #endregion


    /* -------- ③ Action Choice UI -------- */
    #region ▶ Additional Choice  팝업

        private void ShowAdditionalActionChoices(GameActionType type, List<GameAction> choices)
        {
            if (additionalChoicesContainer != null)
            {
                Destroy(additionalChoicesContainer);
                additionalChoicesContainer = null;
            }

            choices.Sort((a, b) => ((int)a.Tile).CompareTo((int)b.Tile));

            // 1) 컨테이너 + 배경
            additionalChoicesContainer = new GameObject("AdditionalChoicesContainer",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            additionalChoicesContainer.transform.SetParent(actionButtonPanel.parent, false);
            var contRt = additionalChoicesContainer.GetComponent<RectTransform>();
            var bgImg  = additionalChoicesContainer.GetComponent<Image>();
            bgImg.color = new Color(0,0,0,0.5f);

            contRt.anchorMin = contRt.anchorMax = new Vector2(0.5f, 0.3f);
            contRt.pivot     = new Vector2(0.5f, 0.5f);

            // 2) 크기 계산
            float blockH  = 100f;
            float margin  = 20f;
            int   perCnt  = type == GameActionType.CHII ? 3 : 4;

            var groupWs = choices.Select(act =>
            {
                var spr   = Tile2DManager.Instance.get_sprite_by_name(act.Tile.ToCustomString());
                float rat = spr.rect.width / spr.rect.height;
                return blockH * rat * perCnt + margin * 2;
            }).ToList();

            float totalW = groupWs.Sum() + 50f * (choices.Count-1) + margin*2;
            float totalH = blockH + margin*2;
            contRt.sizeDelta = new Vector2(totalW, totalH);

            // 3) Holder
            var holder = new GameObject("ChoicesHolder", typeof(RectTransform));
            holder.transform.SetParent(additionalChoicesContainer.transform, false);
            var hRt = holder.GetComponent<RectTransform>();
            hRt.anchorMin = new Vector2(0,1);
            hRt.anchorMax = new Vector2(0,1);
            hRt.pivot     = new Vector2(0,1);
            hRt.anchoredPosition = new Vector2(margin, -margin);
            hRt.sizeDelta        = new Vector2(totalW-margin*2, blockH);

            var hlg = holder.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing              = 30f;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment         = TextAnchor.UpperLeft;

            // 4) 각 선택지 버튼
            for(int i=0;i<choices.Count;i++)
            {
                var act = choices[i];
                float gW = groupWs[i];

                var choice = new GameObject($"Choice_{i}",
                    typeof(RectTransform), typeof(LayoutElement),
                    typeof(Button), typeof(Image));
                choice.transform.SetParent(holder.transform, false);

                var cRt = choice.GetComponent<RectTransform>();
                cRt.sizeDelta = new Vector2(gW, blockH);
                var le = choice.GetComponent<LayoutElement>();
                le.preferredWidth  = gW;
                le.preferredHeight = blockH;

                var bg = choice.GetComponent<Image>();
                bg.color         = new Color(0,0,0,0);
                bg.raycastTarget = true;

                choice.GetComponent<Button>().onClick.AddListener(()=>{ OnActionButtonClicked(act); });

                float x = 0f;
                for(int j=0;j<perCnt;j++)
                {
                    GameTile tile = (type == GameActionType.CHII)
                                    ? (GameTile)((int)act.Tile + j)
                                    : act.Tile;
                    x = CreateChoiceTileAt(tile, x, blockH, choice.transform);
                }
            }

            // 5) Back  버튼
            if(backButtonPrefab!=null)
            {
                var back = Instantiate(backButtonPrefab, additionalChoicesContainer.transform);
                var bRt  = back.GetComponent<RectTransform>();
                bRt.anchorMin = bRt.anchorMax = new Vector2(1,1);
                bRt.pivot     = new Vector2(1,1);
                bRt.anchoredPosition = new Vector2(-margin,-margin);

                var ig = back.gameObject.AddComponent<LayoutElement>();
                ig.ignoreLayout = true;

                back.GetComponent<Button>().onClick.AddListener(()=>{
                    Destroy(additionalChoicesContainer);
                    actionButtonPanel.gameObject.SetActive(true);
                });
            }
        }

    #endregion


    /* -------- ④ 선택 전송 & Skip -------- */
    #region ▶ Send / Skip Buttons

        private void OnActionButtonClicked(GameAction action)
        {
            Debug.Log($"액션 선택: {action.Type} / 타일: {action.Tile}");
            SendSelectedAction(action);
            ClearActionUI();
        }

        private void SendSelectedAction(GameAction action)
        {
            var payload = new
            {
                action_type = action.Type,
                action_tile = action.Tile,
                action_id   = currentActionId
            };
            GameWS.Instance.SendGameEvent(GameWSActionType.RETURN_ACTION, payload);
        }

        public void OnSkipButtonClicked()
        {
            ClearActionUI();
            Debug.Log("Skip 선택");
            var skip = new GameAction
            {
                Type         = GameActionType.SKIP,
                Tile         = GameTile.M1,
                SeatPriority = RelativeSeat.SELF
            };
            SendSelectedAction(skip);
        }

        public void OnSkipButtonClickedAfterTsumo()
        {
            ClearActionButtons();
            Debug.Log("Skip 선택");
            var skip = new GameAction
            {
                Type         = GameActionType.SKIP,
                Tile         = GameTile.M1,
                SeatPriority = RelativeSeat.SELF
            };
            SendSelectedAction(skip);
        }

    #endregion

#endregion /* 🎯 ACTION PANEL */
    }
}
