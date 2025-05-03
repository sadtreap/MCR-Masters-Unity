using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;
using MCRGame.Common;

namespace MCRGame.Game.Events
{
    public class DrawHandler : MonoBehaviour, IGameEventHandler
    {
        public void Handle(GameEventType evt, JObject data)
        {
            if (evt != GameEventType.NEXT_ROUND_CONFIRM) return;

            var anKanInfos = new List<List<GameTile>>();
            if (data.TryGetValue("an_kan_infos", out var anTok))
                foreach (var arr in anTok.ToObject<List<List<int>>>())
                    anKanInfos.Add(arr.Select(i => (GameTile)i).ToList());

            HandlerUtil.GM.StartCoroutine(HandlerUtil.GM.ProcessDraw(anKanInfos));
        }
    }
}
