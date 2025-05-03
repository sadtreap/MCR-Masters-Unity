using Newtonsoft.Json.Linq;
using UnityEngine;
using MCRGame.Common;

namespace MCRGame.Game.Events
{
    public class DiscardHandler : MonoBehaviour, IGameEventHandler
    {
        public void Handle(GameEventType evt, JObject data)
        {
            // 로비용 ROBBING_KONG_ACTIONS 도 동일 포맷
            bool hasActionList = data.ContainsKey("actions");

            if (hasActionList)
            {
                // 선택지 패킷 → UI 에 액션 버튼 표시
                HandlerUtil.GM.ProcessDiscardActions(data);
            }
            else
            {
                // 실제 버림 확정 패킷
                HandlerUtil.GM.ConfirmDiscard(data);
            }
        }
    }

}
