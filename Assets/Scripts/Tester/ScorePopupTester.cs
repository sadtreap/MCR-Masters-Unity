using UnityEngine;
using MCRGame.Common;
using System.Collections.Generic;
using MCRGame.UI;

namespace MCRGame.Tester
{
    public class ScorePopupTester : MonoBehaviour
    {
        private bool showTestUI = true;
        private WinningScoreData testData;
        private Vector2 scrollPosition;

        private void OnGUI()
        {
            if (!showTestUI) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, Screen.height - 20));
            GUILayout.BeginVertical("box");

            GUILayout.Label("Score Popup Tester", GUILayout.Height(30));

            if (GUILayout.Button("Create Test Data"))
            {
                CreateTestData();
            }

            if (GUILayout.Button("Show Popup"))
            {
                if (testData == null)
                {
                    CreateTestData();
                }
                ScorePopupManager.Instance.ShowWinningPopup(testData);
            }

            if (GUILayout.Button("Close All Popups"))
            {
                ScorePopupManager.Instance.DeleteWinningPopup();
            }

            GUILayout.Space(20);
            GUILayout.Label("Current Test Data:");

            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
            if (testData != null)
            {
                GUILayout.Label($"Winner Seat: {testData.winnerSeat}");
                GUILayout.Label($"Single Score: {testData.singleScore:N0}");
                GUILayout.Label($"Total Score: {testData.totalScore:N0}");
                GUILayout.Label($"Flower Count: {testData.flowerCount}");

                GUILayout.Space(10);
                GUILayout.Label("Yaku Scores:");
                foreach (var yaku in testData.yaku_score_list)
                {
                    GUILayout.Label($"- {yaku.YakuId}: {yaku.Score:N0}점");
                }

                GUILayout.Space(10);
                GUILayout.Label("Call Blocks:");
                foreach (var block in testData.callBlocks)
                {
                    GUILayout.Label($"- {block}");
                }

                GUILayout.Space(10);
                GUILayout.Label("Winning Hand:");
                foreach (var tile in testData.handTiles)
                {
                    GUILayout.Label($"- {tile}");
                }
            }
            else
            {
                GUILayout.Label("No test data created yet");
            }
            GUILayout.EndScrollView();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void CreateTestData()
        {
            testData = new WinningScoreData
            {
                winnerSeat = 0,
                singleScore = 66,
                totalScore = 248,
                flowerCount = 3,
                yaku_score_list = new List<YakuScore>
                {
                    new YakuScore { YakuId = Yaku.FourKongs, Score = 11 },
                    new YakuScore { YakuId = Yaku.AllPungs, Score = 22 },
                    new YakuScore { YakuId = Yaku.AllHonors, Score = 33 },
                    new YakuScore { YakuId = Yaku.AllHonors, Score = 33 },
                    new YakuScore { YakuId = Yaku.AllHonors, Score = 33 },
                    new YakuScore { YakuId = Yaku.AllHonors, Score = 33 },
                    new YakuScore { YakuId = Yaku.AllHonors, Score = 33 },
                    new YakuScore { YakuId = Yaku.AllHonors, Score = 33 }
                },
                callBlocks = new List<CallBlockData>
                {
                    new CallBlockData
                    {
                        Type = CallBlockType.PUNG, // 또는 CHII, AN_KONG 등
                        FirstTile = GameTile.M1,   // 시작 타일 (예: 만1)
                        SourceSeat = RelativeSeat.KAMI, // 상대 좌석 (KAMI, TOI, SHIMO, SELF)
                        SourceTileIndex = 1        // 소스 타일 인덱스
                    },
                    new CallBlockData
                    {
                        Type = CallBlockType.PUNG, // 또는 CHII, AN_KONG 등
                        FirstTile = GameTile.M2,   // 시작 타일 (예: 만1)
                        SourceSeat = RelativeSeat.KAMI, // 상대 좌석 (KAMI, TOI, SHIMO, SELF)
                        SourceTileIndex = 1        // 소스 타일 인덱스
                    },
                    new CallBlockData
                    {
                        Type = CallBlockType.PUNG, // 또는 CHII, AN_KONG 등
                        FirstTile = GameTile.M3,   // 시작 타일 (예: 만1)
                        SourceSeat = RelativeSeat.KAMI, // 상대 좌석 (KAMI, TOI, SHIMO, SELF)
                        SourceTileIndex = 1        // 소스 타일 인덱스
                    },
                    new CallBlockData
                    {
                        Type = CallBlockType.PUNG, // 또는 CHII, AN_KONG 등
                        FirstTile = GameTile.M4,   // 시작 타일 (예: 만1)
                        SourceSeat = RelativeSeat.TOI, // 상대 좌석 (KAMI, TOI, SHIMO, SELF)
                        SourceTileIndex = 1        // 소스 타일 인덱스
                    },
                },
                handTiles = new List<GameTile>
                {
                    GameTile.M5, GameTile.M5
                },
                winningTile = GameTile.M5
            };
        }
    }
}