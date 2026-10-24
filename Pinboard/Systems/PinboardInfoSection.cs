using Colossal.UI.Binding;
using Game.Prefabs;
using Game.UI.InGame;
using Unity.Entities;

namespace Pinboard.Systems
{
    // Adds a "Pinboard" section to the panel that opens on the side when you click
    // something in the city (a school, a tree, a road...), so it can be pinned from
    // there. This class only tells the game when to show the section; the buttons
    // are drawn by the panel code, and the pinning is done by PinboardUISystem.
    public partial class PinboardInfoSection : InfoSectionBase
    {
        // The name the screen side uses to find its part of the section.
        public const string kGroup = "Pinboard.Systems.PinboardInfoSection";

        private Entity m_LastPrefab = Entity.Null;

        protected override string group => kGroup;

        protected override void OnCreate()
        {
            base.OnCreate();
            World.GetOrCreateSystemManaged<SelectedInfoUISystem>().AddMiddleSection(this);
        }

        protected override void Reset() { }

        protected override void OnProcess() { }

        public override void OnWriteProperties(IJsonWriter writer) { }

        protected override void OnUpdate()
        {
            base.OnUpdate();

            // What the clicked thing is built from (its "prefab"), if anything.
            var prefab = Entity.Null;
            var selected = selectedEntity;
            if (selected != Entity.Null && EntityManager.Exists(selected) && EntityManager.HasComponent<PrefabRef>(selected))
                prefab = EntityManager.GetComponentData<PrefabRef>(selected).m_Prefab;

            if (prefab != m_LastPrefab)
            {
                m_LastPrefab = prefab;
                PinboardUISystem.Instance?.SetInspectedPrefab(prefab);
            }
            visible = prefab != Entity.Null && PinboardUISystem.Instance != null && PinboardUISystem.Instance.HasInspected;
        }
    }
}
