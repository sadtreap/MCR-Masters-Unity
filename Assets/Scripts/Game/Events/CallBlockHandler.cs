using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using MCRGame.Common;

namespace MCRGame.Game.Events
{
    public class CallBlockHandler : MonoBehaviour, IGameEventHandler
    {
        private static readonly HashSet<GameEventType> Targets = new()
        {
            GameEventType.CHII,
            GameEventType.PON,
            GameEventType.DAIMIN_KAN,
            GameEventType.SHOMIN_KAN,
            GameEventType.AN_KAN
        };

        public void Handle(GameEventType evt, JObject data)
        {
            if (!Targets.Contains(evt)) return;
            HandlerUtil.GM.ConfirmCallBlock(data);
        }
    }
}
