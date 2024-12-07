﻿using Spirit604.DotsCity.Hybrid.Core;
using System;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Gameplay.Player
{
    public class PlayerActorTracker : MonoBehaviour
    {
        private Transform actor;
        private IHybridEntityRef playerHybridEntityRef;

        public Transform Actor
        {
            get
            {
                return actor;
            }

            set
            {
                if (actor != value)
                {
                    actor = value;

                    if (actor != null)
                    {
                        playerHybridEntityRef = actor.GetComponent<IHybridEntityRef>();
                    }
                    else
                    {
                        playerHybridEntityRef = null;
                    }

                    OnSwitchActor(value);
                }
            }
        }

        public Entity PlayerEntity => playerHybridEntityRef != null ? playerHybridEntityRef.RelatedEntity : Entity.Null;

        public event Action<Transform> OnSwitchActor = delegate { };
    }
}