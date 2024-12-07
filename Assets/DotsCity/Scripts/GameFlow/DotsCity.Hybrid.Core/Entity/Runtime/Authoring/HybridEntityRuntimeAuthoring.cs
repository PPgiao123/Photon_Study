using Spirit604.DotsCity.Core;
using Spirit604.Extensions;
using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Spirit604.DotsCity.Hybrid.Core
{
    [DisallowMultipleComponent]
    public class HybridEntityRuntimeAuthoring : RuntimeEntityAuthoringBase, IRuntimeEntityComponentSetProvider, IRuntimeInitEntity, IHybridEntityRef
    {
        [Tooltip("If toggled dispose custom logic should be provided by user. Subscribe to 'OnDisposeRequested' event in RuntimeHybridEntityService")]
        [SerializeField] private bool customDispose;

        [SerializeField] private List<HybridComponentBase> hybridComponents = new List<HybridComponentBase>();

        public bool CustomDispose => customDispose;
        public Entity RelatedEntity => Entity;
        public bool Destroyed { get; set; }

        public ComponentType[] GetComponentSet() => new ComponentType[] { typeof(WorldEntitySharedType), typeof(PooledEventTag), typeof(PoolableTag) };

        public event Action<Entity> OnEntityInitialized = delegate { };

        private void OnEnable()
        {
            InitEntity();
            RuntimeHybridEntityService.Instance.RegisterEntity(this);
            Destroyed = false;
        }

        private void OnDisable()
        {
            if (RuntimeHybridEntityService.Instance)
                RuntimeHybridEntityService.Instance.UnregisterEntity(this);

            if (!Destroyed)
            {
                try
                {
                    DestroyEntity();
                }
                catch { }
            }
            else
            {
                Destroyed = false;
            }
        }

        public void Initialize(EntityManager entityManager, GameObject root, Entity entity)
        {
            entityManager.SetComponentEnabled<PooledEventTag>(entity, false);
            entityManager.SetSharedComponent(entity, new WorldEntitySharedType()
            {
                EntityWorldType = EntityWorldType.HybridRuntimeEntity
            });
        }

        void IHybridEntityRef.DestroyEntity()
        {
            base.DestroyEntity();
        }

        protected override void PostEntityInit(Entity entity)
        {
            OnEntityInitialized(entity);
        }

        public void AddHybridComponent<T>() where T : HybridComponentBase
        {
            var component = ScriptableObject.CreateInstance<T>();
            hybridComponents.Add(component);
        }

        protected override List<ComponentType> GetCustomTypeList()
        {
            List<ComponentType> list = new List<ComponentType>();

            for (int i = 0; i < hybridComponents.Count; i++)
            {
                if (hybridComponents[i] is IRuntimeEntityComponentSetProvider)
                {
                    var provider = hybridComponents[i] as IRuntimeEntityComponentSetProvider;

                    var comps = provider.GetComponentSet();

                    for (int j = 0; j < comps?.Length; j++)
                    {
                        list.TryToAdd(comps[j]);
                    }
                }
            }

            return list;
        }

        protected override IRuntimeInitEntity[] GetInitializers()
        {
            List<IRuntimeInitEntity> inits = new List<IRuntimeInitEntity>();

            var originInits = base.GetInitializers();

            if (originInits?.Length > 0)
            {
                inits.AddRange(originInits);
            }

            for (int i = 0; i < hybridComponents.Count; i++)
            {
                if (hybridComponents[i] is IRuntimeInitEntity)
                {
                    inits.Add(hybridComponents[i] as IRuntimeInitEntity);
                }
            }

            return inits.ToArray();
        }
    }
}
