using System;
using System.Collections.Generic;
using UnityEngine;

namespace Escape.SceneObject.Elias.Prepare
{
    [Serializable]
    public class PlantRoute
    {
        public string RouteName;

        [Tooltip("このルートに必要な手順のリスト（上から順に一致するか確認）")]
        public List<BoosterActionType> RequiredSequence;

        public Sprite GrowingSprite;
        public Sprite MatureSprite;
        public ItemType ResultItem;
    }

    [CreateAssetMenu(fileName = "NewPlantRouteData", menuName = "Gimmick/PlantRouteData")]
    public class PlantRouteData : ScriptableObject
    {
        [Tooltip("3つの植物になるルート等を設定。")] public List<PlantRoute> Routes = new List<PlantRoute>();
    }
}