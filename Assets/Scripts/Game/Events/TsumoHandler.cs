using Newtonsoft.Json.Linq;
using UnityEngine;
using MCRGame.Common;

namespace MCRGame.Game.Events
{
    public class TsumoHandler : MonoBehaviour, IGameEventHandler
    {
        public void Handle(GameEventType evt, JObject data)
        {
            if (evt != GameEventType.TSUMO) return;

            if (data.ContainsKey("action_id"))
                HandlerUtil.GM.StartCoroutine(HandlerUtil.GM.WaitAndProcessTsumo(data));
            else
                HandlerUtil.GM.ConfirmTsumo(data);
        }
    }
}
