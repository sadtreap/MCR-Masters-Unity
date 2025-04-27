using TMPro;
using UnityEngine;

namespace MCRGame.UI
{
    public class YakuObject : MonoBehaviour
    {
        [SerializeField] private TMP_Text yakuNameText;
        [SerializeField] private TMP_Text scoreText;


        public void SetYakuInfo(string yakuName, string score)
        {
            yakuNameText.text = yakuName;
            scoreText.text = score;
        }
    }
}
