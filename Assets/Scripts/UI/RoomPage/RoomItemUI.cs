using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MCRGame.Net;


namespace MCRGame.UI
{
    public class RoomItemUI : MonoBehaviour
    {
        [SerializeField] private Text titleText;
        [SerializeField] private Text infoText;
        [SerializeField] private Button joinButton;
        private RoomInfo roomInfo;

        private void Awake()
        {
            joinButton.onClick.AddListener(OnJoinClicked);
        }

        public void Setup(RoomInfo info)
        {
            roomInfo = info;
            titleText.text = info.name;
            infoText.text = $"{info.current_users}/{info.max_users} | Host: {info.host_nickname}";
        }

        private void OnJoinClicked()
        {
            RoomService.Instance.JoinRoom(roomInfo.room_number);
            SceneManager.LoadScene("RoomScene", LoadSceneMode.Single);
        }
    }
}