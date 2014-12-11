﻿using UnityEngine;
using System.Collections;
using Mothership;

namespace MothershipStateMachine
{
    public class ServerGameEndState : ServerGameState
    {
        public ServerGameEndState(ServerManager manager) : base(manager)
        {

        }

        public override void OnGameMessage(GameMessage message)
        {
            base.OnGameMessage(message);
        }

        public override void OnStateMessage(StateMessage message)
        {
            OnEnterState enter = message as OnEnterState;
            if(enter != null)
            {
                serverManager.networkManager.MatchExpired();
                return;
            }
        }
    }
    
}