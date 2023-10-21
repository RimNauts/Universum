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
        public bool keepAfterAbandon;
        public bool hideIcon;

        private string _exposeCelestialObjectDefName;
        private int? _exposeCelestialObjectSeed;
        private Vector3? _exposeCelestialObjectPosition;
        private int? _exposeCelestialObjectDeathTick;

        public override string Label => _celestialObject.name;
        public override Vector3 DrawPos => _celestialObject.transformedPosition;
        public override Texture2D ExpandingIcon => HasMap ? _overlayIcon : base.ExpandingIcon;
        public override MapGeneratorDef MapGeneratorDef => _celestialObjectDef.objectHolder.mapGeneratorDef;

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
            keepAfterAbandon = _celestialObjectDef.objectHolder.keepAfterAbandon;

            _celestialObject.objectHolder = this;
        }

        public override void Destroy() {
            if (!Destroyed) base.Destroy();
        }

        public void SignalDestruction() {
            _celestialObject.forceDeath = true;
            Game.MainLoop.instance.dirtyCache = true;
        }

        public override void PostRemove() {
            base.PostRemove();
            if (keepAfterAbandon) {
                ObjectHolder newObjectHolder = Generator.CreateObjectHolder(_celestialObjectDef.defName, celestialObject: _celestialObject);
                newObjectHolder.Tile = Tile;
            } else {
                Generator.UpdateTile(Tile, Assets.oceanBiomeDef);
            }
        }

        public void Randomize() => _celestialObject.Randomize();

        public override void Tick() { }

        public override void Draw() { }

        public override void Print(LayerSubMesh subMesh) { }

        public Map Settle(RimWorld.Planet.Caravan caravan) {
            if (caravan.Faction != RimWorld.Faction.OfPlayer) return null;
            // handle colonist memories
            if (Find.AnyPlayerHomeMap == null) {
                foreach (Pawn podsAliveColonist in RimWorld.PawnsFinder.AllMapsCaravansAndTravelingTransportPods_Alive_Colonists) {
                    RimWorld.MemoryThoughtHandler memories = podsAliveColonist.needs?.mood?.thoughts?.memories;
                    if (memories != null) {
                        memories.RemoveMemoriesOfDef(RimWorld.ThoughtDefOf.NewColonyOptimism);
                        memories.RemoveMemoriesOfDef(RimWorld.ThoughtDefOf.NewColonyHope);
                        if (podsAliveColonist.IsFreeNonSlaveColonist) memories.TryGainMemory(RimWorld.ThoughtDefOf.NewColonyOptimism);
                    }
                }
            }
            // generate map
            Map map = CreateMap(caravan.Faction);
            if (map == null) return null;
            // spawn colonist
            LongEventHandler.QueueLongEvent(() => {
                Pawn pawn = caravan.PawnsListForReading[0];
                RimWorld.Planet.CaravanEnterMapUtility.Enter(
                    caravan,
                    map,
                    RimWorld.Planet.CaravanEnterMode.Center,
                    RimWorld.Planet.CaravanDropInventoryMode.DropInstantly,
                    extraCellValidator: x => x.GetRoom(map).CellCount >= 600
                );
                CameraJumper.TryJump(pawn);
            }, "SpawningColonists", true, new Action<Exception>(GameAndMapInitExceptionHandlers.ErrorWhileGeneratingMap));

            return map;
        }

        public Map CreateMap(RimWorld.Faction faction) {
            if (faction != RimWorld.Faction.OfPlayer) return null;
            Map map = MapGenerator.GenerateMap(Find.World.info.initialMapSize, this, MapGeneratorDef, ExtraGenStepDefs, extraInitBeforeContentGen: null);
            SetFaction(faction);
            Find.World.WorldUpdate();
            return map;
        }

        public void CheckHideIcon() {
            hideIcon = Patch.WorldRendererUtility.ShouldHideObjectHolder(DrawPos);
        }

        public bool SafeDespawn() {
            if (HasMap) return false;
            if (_AnyTravelingTransportPodsHere() || _AnyCaravansHere()) return false;

            return true;
        }

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject) {
            alsoRemoveWorldObject = false;

            if (_AnyTravelingTransportPodsHere() || _AnyCaravansHere()) return false;

            return base.ShouldRemoveMapNow(out alsoRemoveWorldObject);
        }

        private bool _AnyTravelingTransportPodsHere() {
            bool IsMatchingPod(TravelingTransportPods pods) => pods.initialTile == Tile || pods.destinationTile == Tile;
            return Find.World.worldObjects.AllWorldObjects.OfType<RimWorld.Planet.TravelingTransportPods>().Any(IsMatchingPod);
        }

        private bool _AnyCaravansHere() {
            bool isMatchingPawn(RimWorld.Planet.Caravan caravan) => caravan.Tile == Tile;
            return Find.World.worldObjects.AllWorldObjects.OfType<RimWorld.Planet.Caravan>().Any(isMatchingPawn);
        }

        private string _GetDeathTimerLabel() {
            if (_celestialObject.deathTick == null || !SafeDespawn()) return "";

            float timeLeft = (float) _celestialObject.deathTick - Game.MainLoop.instance.tick;
            if (timeLeft < 60000.0f) {
                return "RimNauts.hours_left".Translate(Math.Ceiling(timeLeft / 2500.0f).ToString());
            } else return "RimNauts.days_left".Translate((timeLeft / 60000.0f).ToString("0.00"));
        }

        public override string GetDescription() {
            StringBuilder stringBuilder = new StringBuilder();
            _AppendCelestialDescription(stringBuilder);
            _AppendComponentDescriptionParts(stringBuilder);

            return stringBuilder.ToString();
        }

        public override string GetInspectString() {
            StringBuilder stringBuilder = new StringBuilder();
            _AppendCelestialDescription(stringBuilder);
            _AppendFactionIfApplicable(stringBuilder);
            _AppendComponentStrings(stringBuilder);
            RimWorld.QuestUtility.AppendInspectStringsFromQuestParts(stringBuilder, this);
            _AppendCooldownInformation(stringBuilder);
            _AppendConditionCausersIfPresent(stringBuilder);

            return stringBuilder.ToString();
        }

        private void _AppendCelestialDescription(StringBuilder sb) {
            sb.Append(_celestialObjectDef.objectHolder.description);
            sb.Append(_GetDeathTimerLabel());
        }

        private void _AppendFactionIfApplicable(StringBuilder sb) {
            if (Faction != null && AppendFactionToInspectString) {
                sb.AppendLine("Faction".Translate() + ": " + Faction.Name);
            }
        }

        private void _AppendComponentDescriptionParts(StringBuilder sb) {
            for (int i = 0; i < comps.Count; i++) {
                string descriptionPart = comps[i].GetDescriptionPart();
                if (!descriptionPart.NullOrEmpty()) {
                    if (sb.Length > 0) {
                        sb.AppendLine();
                        sb.AppendLine();
                    }
                    sb.Append(descriptionPart);
                }
            }
        }

        private void _AppendComponentStrings(StringBuilder sb) {
            for (int i = 0; i < comps.Count; i++) {
                string text = comps[i].CompInspectStringExtra();
                if (!text.NullOrEmpty()) {
                    if (Prefs.DevMode && char.IsWhiteSpace(text[text.Length - 1])) {
                        Log.ErrorOnce(string.Concat(comps[i].GetType(), " CompInspectStringExtra ended with whitespace: ", text), 25612);
                        text = text.TrimEndNewlines();
                    }

                    if (sb.Length != 0) {
                        sb.AppendLine();
                    }

                    sb.Append(text);
                }
            }
        }

        private void _AppendCooldownInformation(StringBuilder sb) {
            if (this.EnterCooldownBlocksEntering()) {
                if (sb.Length > 0) {
                    sb.AppendLine();
                }
                sb.AppendLine("EnterCooldown".Translate(this.EnterCooldownTicksLeft().ToStringTicksToPeriod()));
            }
        }

        private void _AppendConditionCausersIfPresent(StringBuilder sb) {
            if (!HandlesConditionCausers && HasMap) {
                List<Thing> list = Map.listerThings.ThingsInGroup(ThingRequestGroup.ConditionCauser);
                for (int i = 0; i < list.Count; i++) {
                    sb.AppendLine(list[i].LabelShortCap + " (" + "ConditionCauserRadius".Translate(list[i].TryGetComp<RimWorld.CompCauseGameCondition>().Props.worldRange) + ")");
                }
            }
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
