using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace Universum.World {
    [StaticConstructorOnStartup]
    public class ObjectHolder : RimWorld.Planet.MapParent {
        private CelestialObject _celestialObject;
        private Defs.CelestialObject _celestialObjectDef;

        private Texture2D _overlayIcon;

        private string _exposeCelestialObjectDefName;
        private int? _exposeCelestialObjectSeed;
        private Vector3? _exposeCelestialObjectPosition;
        private int? _exposeCelestialObjectDeathTick;

        public void Init(
            string celestialObjectDefName,
            int? celestialObjectSeed = null,
            Vector3? celestialObjectPosition = null,
            int? celestialObjectDeathTick = null,
            CelestialObject celestialObject = null
        ) {
            _celestialObject = celestialObject ?? Generator.Create(celestialObjectDefName, celestialObjectSeed, celestialObjectPosition, celestialObjectDeathTick);
            _celestialObjectDef = Defs.Loader.celestialObjects[celestialObjectDefName];

            _overlayIcon = Assets.GetTexture(_celestialObjectDef.objectHolder.overlayIconPath);

            _celestialObject.objectHolder = this;
        }

        public override void PostRemove() {
            base.PostRemove();
            if (_celestialObjectDef.objectHolder.keepAfterAbandon) {
                ObjectHolder newObjectHolder = Generator.CreateObjectHolder(_celestialObjectDef.defName, celestialObject: _celestialObject);
                newObjectHolder.Tile = Tile;
            } else {
                Generator.TileClear(Tile);
            }
        }

        public override void Tick() { }

        public override void Draw() { }

        public override void Print(LayerSubMesh subMesh) { }

        public override Vector3 DrawPos => _celestialObject.transformedPosition;

        public override Texture2D ExpandingIcon => HasMap ? _overlayIcon : base.ExpandingIcon;

        public override MapGeneratorDef MapGeneratorDef => _celestialObjectDef.objectHolder.mapGeneratorDef;

        public override string Label => _celestialObject.name;

        public override string GetDescription() {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(_celestialObjectDef.objectHolder.description);

            for (int i = 0; i < comps.Count; i++) {
                string descriptionPart = comps[i].GetDescriptionPart();
                if (!descriptionPart.NullOrEmpty()) {
                    if (stringBuilder.Length > 0) {
                        stringBuilder.AppendLine();
                        stringBuilder.AppendLine();
                    }
                    stringBuilder.Append(descriptionPart);
                }
            }

            return stringBuilder.ToString();
        }

        public override string GetInspectString() {
            StringBuilder stringBuilder = new StringBuilder();

            stringBuilder.Append(_celestialObjectDef.objectHolder.description);

            if (Faction != null && AppendFactionToInspectString) {
                stringBuilder.Append("Faction".Translate() + ": " + Faction.Name);
            }

            for (int i = 0; i < comps.Count; i++) {
                string text = comps[i].CompInspectStringExtra();
                if (!text.NullOrEmpty()) {
                    if (Prefs.DevMode && char.IsWhiteSpace(text[text.Length - 1])) {
                        Log.ErrorOnce(string.Concat(comps[i].GetType(), " CompInspectStringExtra ended with whitespace: ", text), 25612);
                        text = text.TrimEndNewlines();
                    }

                    if (stringBuilder.Length != 0) {
                        stringBuilder.AppendLine();
                    }

                    stringBuilder.Append(text);
                }
            }

            RimWorld.QuestUtility.AppendInspectStringsFromQuestParts(stringBuilder, this);

            string restText = stringBuilder.ToString();

            if (this.EnterCooldownBlocksEntering()) {
                if (!restText.NullOrEmpty()) {
                    restText += "\n";
                }

                restText += "EnterCooldown".Translate(this.EnterCooldownTicksLeft().ToStringTicksToPeriod());
            }

            if (!HandlesConditionCausers && HasMap) {
                List<Thing> list = Map.listerThings.ThingsInGroup(ThingRequestGroup.ConditionCauser);
                for (int i = 0; i < list.Count; i++) {
                    restText += "\n" + list[i].LabelShortCap + " (" + "ConditionCauserRadius".Translate(list[i].TryGetComp<RimWorld.CompCauseGameCondition>().Props.worldRange) + ")";
                }
            }

            return restText;
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject) {
            alsoRemoveWorldObject = true;
            // predicate function to find matching traveling pods.
            Func<RimWorld.Planet.TravelingTransportPods, bool> IsMatchingPod = (pods) => {
                int initialTile = (int) typeof(RimWorld.Planet.TravelingTransportPods)
                                  .GetField("initialTile", BindingFlags.Instance | BindingFlags.NonPublic)
                                  .GetValue(pods);

                return initialTile == Tile || pods.destinationTile == Tile;
            };
            // check if there are any matching traveling pods in the world.
            if (Find.World.worldObjects.AllWorldObjects.OfType<RimWorld.Planet.TravelingTransportPods>().Any(IsMatchingPod)) return false;

            return base.ShouldRemoveMapNow(out alsoRemoveWorldObject);
        }

        public override void ExposeData() {
            base.ExposeData();
            if (Scribe.mode == LoadSaveMode.Saving) _SaveData();
            if (Scribe.mode == LoadSaveMode.LoadingVars) _LoadData();
            if (Scribe.mode == LoadSaveMode.PostLoadInit) _PostLoadData();
        }

        private void _SaveData() {
            _exposeCelestialObjectDefName = _celestialObject.def.defName;
            _exposeCelestialObjectSeed = _celestialObject.seed;
            _exposeCelestialObjectPosition = _celestialObject.position;
            _exposeCelestialObjectDeathTick = _celestialObject.deathTick;

            Scribe_Values.Look(ref _exposeCelestialObjectDefName, "_exposeCelestialObjectDefName");
            Scribe_Values.Look(ref _exposeCelestialObjectSeed, "_exposeCelestialObjectSeed");
            Scribe_Values.Look(ref _exposeCelestialObjectPosition, "_exposeCelestialObjectPosition");
            Scribe_Values.Look(ref _exposeCelestialObjectDeathTick, "_exposeCelestialObjectDeathTick");
        }

        private void _LoadData() {
            Scribe_Values.Look(ref _exposeCelestialObjectDefName, "_exposeCelestialObjectDefName");
            Scribe_Values.Look(ref _exposeCelestialObjectSeed, "_exposeCelestialObjectSeed");
            Scribe_Values.Look(ref _exposeCelestialObjectPosition, "_exposeCelestialObjectPosition");
            Scribe_Values.Look(ref _exposeCelestialObjectDeathTick, "_exposeCelestialObjectDeathTick");
        }

        private void _PostLoadData() {
            Init(_exposeCelestialObjectDefName, _exposeCelestialObjectSeed, _exposeCelestialObjectPosition, _exposeCelestialObjectDeathTick);
        }
    }
}
