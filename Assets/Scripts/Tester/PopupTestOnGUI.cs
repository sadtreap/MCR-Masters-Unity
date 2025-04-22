using System.Collections.Generic;
using UnityEngine;
using MCRGame.Common;
using MCRGame.Game;
using MCRGame.UI;

public class EndScorePopupTester : MonoBehaviour
{
    [Header("EndScorePopup Prefab (Canvas 포함)")]
    [SerializeField] private GameObject popupPrefab;

    private Rect buttonRect = new Rect(20, 20, 40, 200);

    private void OnGUI()
    {
        if (GUI.Button(buttonRect, "Test End Score Popup"))
        {
            if (popupPrefab == null)
            {
                Debug.LogError("EndScorePopupTester: popupPrefab이 할당되지 않았습니다.");
                return;
            }

            // 1) Prefab 인스턴스화
            GameObject popupGO = Instantiate(popupPrefab);

            // 2) 자식까지 뒤져서 EndScorePopupManager 찾기
            var mgr = popupGO.GetComponentInChildren<EndScorePopupManager>();
            if (mgr == null)
            {
                Debug.LogError("EndScorePopupTester: 자식에서 EndScorePopupManager를 찾을 수 없습니다.");
                return;
            }

            // 3) 실제 게임 데이터가 있으면 사용, 없으면 더미 데이터 생성
            List<Player> players;
            if (GameManager.Instance != null && GameManager.Instance.Players != null && GameManager.Instance.Players.Count > 0)
            {
                players = GameManager.Instance.Players;
            }
            else
            {
                players = new List<Player>
                {
                    new Player("uid1", "Alice", 0, 250),
                    new Player("uid2", "Bob",   1, 180),
                    new Player("uid3", "Carol", 2, 120),
                    new Player("uid4", "Dave",  3,  60),
                };
            }

            // 4) 점수 표시
            StartCoroutine(mgr.ShowScores(players));
        }
    }
}
