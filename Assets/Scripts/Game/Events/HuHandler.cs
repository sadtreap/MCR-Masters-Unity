using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using MCRGame.Common;

namespace MCRGame.Game.Events
{
    public class HuHandler : MonoBehaviour, IGameEventHandler
    {
        public void Handle(GameEventType evt, JObject data)
        {
            if (evt != GameEventType.HU) return;

            try
            {
                var handTiles   = data["hand"].ToObject<List<int>>().Select(i => (GameTile)i).ToList();
                var callBlocks  = data["call_blocks"].ToObject<List<CallBlockData>>();
                var scoreResult = data["score_result"].ToObject<ScoreResult>();
                var winSeat     = data["player_seat"].ToObject<AbsoluteSeat>();
                var curSeat     = data["current_player_seat"].ToObject<AbsoluteSeat>();
                int flowerCnt   = data["flower_count"].ToObject<int>();
                GameTile? tsumo = data["tsumo_tile"].Type == JTokenType.Null ? null
                                  : (GameTile?)data["tsumo_tile"].ToObject<int>();
                var winTile     = (GameTile)data["winning_tile"].ToObject<int>();

                var anKanInfos = new List<List<GameTile>>();
                if (data.TryGetValue("an_kan_infos", out var anTok))
                    foreach (var arr in anTok.ToObject<List<List<int>>>())
                        anKanInfos.Add(arr.Select(i => (GameTile)i).ToList());

                HandlerUtil.GM.StartCoroutine(
                    HandlerUtil.GM.ProcessHuHand(handTiles, callBlocks, scoreResult,
                                                 winSeat, curSeat, flowerCnt,
                                                 tsumo, anKanInfos, winTile));
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HuHandler] parse error: {ex}");
            }
        }
    }
}
