
using TMPro;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;
namespace kleinba.Talos
{
    public class Terminal : UdonSharpBehaviour
    {
        public TextAsset[] topics;
        public GameObject screen;
        public TextMeshProUGUI text;

        public GameObject[] buttons;
        private string toPrint = "";

        private TerminalState state;
        private int selection = -1;

        public override void OnPlayerTriggerEnter(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                state = TerminalState.StartingUp;
                screen.SetActive(true);
                _WhileActive();
            }
        }

        public override void OnPlayerTriggerExit(VRCPlayerApi player)
        {
            if (player.isLocal)
            {
                toPrint = "";
                text.text = "";
                state = TerminalState.Off;
                SendCustomEventDelayedFrames(nameof(_TurnOff), 2);
            }
        }

        public void _TurnOff()
        {
            screen.SetActive(false);
            foreach (GameObject go in buttons)
            {
                go.SetActive(false);
            }
        }

        public void _WhileActive()
        {
            if (text.text.Length < toPrint.Length)
            {
                text.text = toPrint.Substring(0, Mathf.Min(text.text.Length + 1, toPrint.Length));
            }

            //if(text.text.Length == toPrint.Length)
            //{
            switch (state)
            {
                case TerminalState.StartingUp:
                    text.text = "";
                    toPrint = $"There are {topics.Length} topic/s available \n\n";
                    foreach (TextAsset asset in topics)
                    {
                        toPrint += $"- {asset.name}\n";
                    }
                    state = TerminalState.InSelectionMenu;

                    //TODO Show selection buttons for each topic
                    for (int i = 0; i < buttons.Length; i++)
                    {
                        buttons[i].SetActive(i < topics.Length);
                        if (i < topics.Length)
                        {
                            buttons[i].GetComponentInChildren<TextMeshProUGUI>().text = topics[i].name;
                        }

                    }

                    selection = -1;

                    break;
                case TerminalState.InSelectionMenu:
                    if ((selection >= 0) && (selection < topics.Length))
                    {

                        state = TerminalState.SelectedTopic;
                        text.text = "";
                        toPrint = $"Selected topic: {topics[selection].name} \n\n";
                        toPrint += topics[selection].text;

                        selection = -1;


                        buttons[0].GetComponentInChildren<TextMeshProUGUI>().text = "Return";
                        for (int i = 0; i < buttons.Length; i++)
                        {
                            buttons[i].SetActive(i < 1);
                        }
                    }
                    break;
                case TerminalState.SelectedTopic:
                    if (selection != -1)
                    {
                        state = TerminalState.StartingUp;
                    }
                    break;
            }
            //}



            if (state != TerminalState.Off)
            {
                SendCustomEventDelayedFrames(nameof(_WhileActive), 1);
            }
        }

        public void _MakeSelection(int value)
        {
            selection = value;
        }
    }

    public enum TerminalState
    {
        StartingUp,
        InSelectionMenu,
        SelectedTopic,
        ShuttingDown,
        Off
    }
}