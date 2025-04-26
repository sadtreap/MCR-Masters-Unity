using UnityEngine;
using UnityEngine.UI;
using MCRGame.Net;


namespace MCRGame.UI
{
    public class PlayerSlotUI : MonoBehaviour
    {
        [SerializeField] private Text nameText;
        [SerializeField] private Image readyIndicator;
        [SerializeField] private Image characterImage;
        public string Uid { get; private set; }

        public void Setup(RoomUserInfo info, string hostUid)
        {
            Uid = info.uid;
            nameText.gameObject.SetActive(true);
            readyIndicator.gameObject.SetActive(true);
            characterImage.gameObject.SetActive(true);

            nameText.text = info.nickname + (info.uid == hostUid ? " (Host)" : "");
            SetReady(info.is_ready);
        }

        public void SetReady(bool ready)
        {
            readyIndicator.color = ready ? Color.green : Color.red;
        }

        public void SetEmpty()
        {
            // 빈 슬롯은 이름/표시 모두 끄거나, 원하는 placeholder를 띄워도 좋습니다.
            nameText.gameObject.SetActive(false);
            readyIndicator.gameObject.SetActive(false);
            characterImage.gameObject.SetActive(false);
        }
    }
}

