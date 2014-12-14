﻿using UnityEngine;
using System.Collections;

namespace MothershipUI
{
    public class ScreenDispatch : MonoBehaviour
    {
        public enum ScreenTarget { MainMenu, Profile, ClientLobby }
        public static ScreenTarget screenToOpen = ScreenTarget.MainMenu;

        [SerializeField]
        private MainMenuGUI mainMenuGUI = null;
        [SerializeField]
        private ProfileGUI profileGUI = null;
        [SerializeField]
        private ClientLobbyGUI clientLobbyGUI = null;

        private void Start()
        {
            switch(screenToOpen)
            {
                case ScreenTarget.MainMenu:
                    mainMenuGUI.EnableScreen();
                    break;
                case ScreenTarget.Profile:
                    profileGUI.EnableScreen();
                    break;
                case ScreenTarget.ClientLobby:
                    clientLobbyGUI.EnableScreen();
                    break;
            }
        }
    } 
}
