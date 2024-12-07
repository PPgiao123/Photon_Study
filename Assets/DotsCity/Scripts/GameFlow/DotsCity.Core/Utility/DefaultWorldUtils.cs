using Unity.Entities;

namespace Spirit604.DotsCity.Core
{
    public static class DefaultWorldUtils
    {
        public static World DefaultWorld => World.DefaultGameObjectInjectionWorld;
        public static WorldUnmanaged WorldUnmanaged => DefaultWorld.Unmanaged;
        public static EntityManager EntityManager => DefaultWorld.EntityManager;

        public static TSystem CreateAndAddSystemManaged<TSystem, TGroup>(bool ignoreDuplicate = false) where TSystem : ComponentSystemBase where TGroup : ComponentSystemGroup
        {
            var system = DefaultWorld.GetExistingSystemManaged(typeof(TSystem));

            if (system != null)
            {
                if (!ignoreDuplicate)
                {
                    UnityEngine.Debug.Log($"DefaultWorldUtils. CreateAndAddSystemManaged system {(system as TSystem).GetType().Name} already exist");
                }

                return system as TSystem;
            }

            system = DefaultWorld.CreateSystemManaged(typeof(TSystem));

            var systemGroup = DefaultWorld.GetOrCreateSystemManaged<TGroup>();
            systemGroup.AddSystemToUpdateList(system);

            return system as TSystem;
        }

        public static void CreateAndAddSystemUnmanaged<TSystem, TGroup>(bool ignoreDuplicate = false) where TSystem : unmanaged, ISystem where TGroup : ComponentSystemGroup
        {
            bool exist = true;

            try
            {
                // Bad way to check existence
                ref var stateRef = ref WorldUnmanaged.GetExistingSystemState<TSystem>();
                var version = stateRef.GlobalSystemVersion;
            }
            catch
            {
                exist = false;
            }

            if (exist)
            {
                if (!ignoreDuplicate)
                {
                    UnityEngine.Debug.Log($"DefaultWorldUtils. CreateAndAddSystemUnmanaged system {(typeof(TSystem)).Name} already exist");
                }

                return;
            }

            var system = DefaultWorld.CreateSystem(typeof(TSystem));
            var systemGroup = DefaultWorld.GetOrCreateSystemManaged<TGroup>();
            systemGroup.AddSystemToUpdateList(system);
        }

        public static void SwitchActiveManagedSystem<T>(bool isEnabled) where T : SystemBase
        {
            var system = DefaultWorld.GetOrCreateSystemManaged<T>();
            system.Enabled = isEnabled;
        }

        public static void SwitchActiveUnmanagedSystem<T>(bool isEnabled) where T : unmanaged, ISystem
        {
            SwitchActiveUnmanagedSystem<T>(WorldUnmanaged, isEnabled);
        }

        public static void SwitchActiveUnmanagedSystem<T>(WorldUnmanaged world, bool isEnabled) where T : unmanaged, ISystem
        {
            ref var system = ref world.GetExistingSystemState<T>();
            system.Enabled = isEnabled;
        }

        public static bool TryToGetConfig<T>(out T config, bool logException = true) where T : unmanaged, IComponentData
        {
            config = default(T);
            var query = EntityManager.CreateEntityQuery(ComponentType.ReadOnly<T>());

            try
            {
                config = query.GetSingleton<T>();
                return true;
            }
            catch (System.Exception ex)
            {
                if (logException)
                    UnityEngine.Debug.LogException(ex);
            }

            return false;
        }
    }
}
