﻿using Spirit604.Attributes;
using Spirit604.DotsCity.Simulation.Car;
using Spirit604.DotsCity.Simulation.Factory.Car;
using Spirit604.DotsCity.Simulation.Traffic.Authoring;
using Spirit604.Extensions;
using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.Simulation.Factory.Traffic
{
    public abstract class TrafficCarPoolBase : MonoBehaviourBase
    {
        public const int DefaultPoolSize = 100;

        [SerializeField] private TrafficSettings trafficSettings;
        [SerializeField] private VehicleDataHolder vehicleDataHolder;
        [SerializeField] private TrafficCarPoolPreset trafficCarPoolPreset;

        private Dictionary<int, ObjectPool> carPools = new Dictionary<int, ObjectPool>();

        public List<CarPrefabPair> CarPrefabs => trafficCarPoolPreset ? trafficCarPoolPreset.PrefabDatas : null;

        public TrafficCarPoolPreset CarPoolPreset { get => trafficCarPoolPreset; set => trafficCarPoolPreset = value; }

        public VehicleDataCollection VehicleDataCollection => vehicleDataHolder?.VehicleDataCollection ?? null;

        protected virtual int PoolSize => trafficSettings?.PreferableCount ?? DefaultPoolSize;

        protected TrafficSettings TrafficSettings => trafficSettings;

        protected virtual void Start()
        {
            carPools = InitPool(VehicleDataCollection, name, CarPrefabs, PoolSize);
        }

        public GameObject GetCarGo(int carModelType)
        {
            if (carPools != null)
            {
                if (carPools.ContainsKey(carModelType))
                {
                    return carPools[carModelType].Pop();
                }
                else
                {
                    UnityEngine.Debug.Log($"{name} pool doesn't have '{VehicleDataCollection.GetName(carModelType)}' Model {carModelType} model.");
                }
            }

            return null;
        }

        public void ClearNulls()
        {
            if (trafficCarPoolPreset)
            {
                trafficCarPoolPreset.ClearNulls();
            }
        }

        public static Dictionary<int, ObjectPool> InitPool(VehicleDataCollection vehicleDataCollection, string name, List<CarPrefabPair> carPrefabs, int poolSize)
        {
            if (carPrefabs == null)
            {
                UnityEngine.Debug.Log($"{name} preset is null.");
                return null;
            }

            Dictionary<int, ObjectPool> carPools = null;

            foreach (var pair in carPrefabs)
            {
                if (pair.HullPrefab == null || pair.EntityPrefab == null)
                {
                    continue;
                }

                var index = vehicleDataCollection.GetVehicleModelIndex(pair.EntityPrefab);

                if (index == -1)
                {
                    continue;
                }

                if (carPools == null)
                {
                    carPools = new Dictionary<int, ObjectPool>();
                }

                ObjectPool objectPool = PoolManager.Instance.PoolForObject(pair.HullPrefab);
                objectPool.preInstantiateCount = poolSize;
                carPools.Add(index, objectPool);
            }

            return carPools;
        }
    }
}