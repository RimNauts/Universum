using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse.Steam;
using Verse;

namespace Universum.World.Patch {
    public class WorldCameraDriver {
        public static void Init(Harmony harmony) {
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_AltitudePercent)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_MinAltitude)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_CurrentZoom)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_WorldCameraDriverOnGUI)).Patch();
            _ = new PatchClassProcessor(harmony, typeof(WorldCameraDriver_Update)).Patch();
        }

        [HarmonyPatch]
        static class WorldCameraDriver_AltitudePercent {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:get_AltitudePercent");

            public static bool Prefix(ref RimWorld.Planet.WorldCameraDriver __instance, ref float __result) {
                __result = Mathf.InverseLerp(RimWorld.Planet.WorldCameraDriver.MinAltitude, CameraInfo.maxAltitude, __instance.altitude);
                return false;
            }
        }

        [HarmonyPatch]
        static class WorldCameraDriver_MinAltitude {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:get_MinAltitude");

            public static bool Prefix(ref float __result) {
                __result = (float) (CameraInfo.minAltitude + (SteamDeck.IsSteamDeck ? 17.0 : 25.0));
                return false;
            }
        }

        [HarmonyPatch]
        static class WorldCameraDriver_CurrentZoom {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:get_CurrentZoom");

            public static bool Prefix(ref RimWorld.Planet.WorldCameraDriver __instance, ref RimWorld.Planet.WorldCameraZoomRange __result) {
                float altitudePercent = __instance.AltitudePercent;
                if ((double) altitudePercent < 0.025 * CameraInfo.zoomEnumMultiplier) {
                    __result = RimWorld.Planet.WorldCameraZoomRange.VeryClose;
                    return false;
                }
                if ((double) altitudePercent < 0.042 * CameraInfo.zoomEnumMultiplier) {
                    __result = RimWorld.Planet.WorldCameraZoomRange.Close;
                    return false;
                }
                __result = (double) altitudePercent < (0.125 * CameraInfo.zoomEnumMultiplier) ? RimWorld.Planet.WorldCameraZoomRange.Far : RimWorld.Planet.WorldCameraZoomRange.VeryFar;
                return false;
            }
        }

        [HarmonyPatch]
        static class WorldCameraDriver_WorldCameraDriverOnGUI {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:WorldCameraDriverOnGUI");

            public static bool Prefix(ref RimWorld.Planet.WorldCameraDriver __instance) {
                _UpdateReleasedLeftWhileHoldingMiddle(ref __instance);
                _UpdateMouseCoveredByUI(ref __instance);

                if (__instance.AnythingPreventsCameraMotion) {
                    return false;
                }

                _HandleMouseDrag(ref __instance);
                _HandleScrollWheelAndZoom(ref __instance);
                _HandleKeyMovements(ref __instance);

                __instance.config.ConfigOnGUI();

                return false;
            }

            private static void _UpdateReleasedLeftWhileHoldingMiddle(ref RimWorld.Planet.WorldCameraDriver __instance) {
                if (Input.GetMouseButtonUp(0) && Input.GetMouseButton(2)) {
                    __instance.releasedLeftWhileHoldingMiddle = true;
                } else if (Event.current.rawType == EventType.MouseDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(2)) {
                    __instance.releasedLeftWhileHoldingMiddle = false;
                }
            }

            private static void _UpdateMouseCoveredByUI(ref RimWorld.Planet.WorldCameraDriver __instance) {
                __instance.mouseCoveredByUI = Find.WindowStack.GetWindowAt(UI.MousePositionOnUIInverted) != null;
            }

            private static void _HandleMouseDrag(ref RimWorld.Planet.WorldCameraDriver __instance) {
                if (!UnityGUIBugsFixer.IsSteamDeckOrLinuxBuild && Event.current.type == EventType.MouseDrag && Event.current.button == 2 ||
                    UnityGUIBugsFixer.IsSteamDeckOrLinuxBuild && Input.GetMouseButton(2) &&
                    (!SteamDeck.IsSteamDeck || !Find.WorldSelector.AnyCaravanSelected)) {
                    Vector2 currentEventDelta = UnityGUIBugsFixer.CurrentEventDelta;

                    if (Event.current.type == EventType.MouseDrag) {
                        Event.current.Use();
                    }

                    if (currentEventDelta != Vector2.zero) {
                        RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.FrameInteraction);

                        currentEventDelta.x *= -1f;
                        __instance.desiredRotationRaw += currentEventDelta / RimWorld.Planet.GenWorldUI.CurUITileSize() * 0.273f * (Prefs.MapDragSensitivity * CameraInfo.dragSensitivityMultiplier);
                    }
                }
            }

            private static void _HandleScrollWheelAndZoom(ref RimWorld.Planet.WorldCameraDriver __instance) {
                float num = 0.0f;

                if (Event.current.type == EventType.ScrollWheel) {
                    num -= Event.current.delta.y * 0.1f;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapZoom_In.KeyDownEvent) {
                    num += 2f;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapZoom_Out.KeyDownEvent) {
                    num -= 2f;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                __instance.desiredAltitude -= num * (__instance.config.zoomSpeed * CameraInfo.zoomSensitivityMultiplier) * __instance.altitude / 12.0f;
                __instance.desiredAltitude = Mathf.Clamp(__instance.desiredAltitude, RimWorld.Planet.WorldCameraDriver.MinAltitude, CameraInfo.maxAltitude);
            }

            private static void _HandleKeyMovements(ref RimWorld.Planet.WorldCameraDriver __instance) {
                __instance.desiredRotation = Vector2.zero;

                if (RimWorld.KeyBindingDefOf.MapDolly_Left.IsDown) {
                    __instance.desiredRotation.x = -__instance.config.dollyRateKeys;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapDolly_Right.IsDown) {
                    __instance.desiredRotation.x = __instance.config.dollyRateKeys;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapDolly_Up.IsDown) {
                    __instance.desiredRotation.y = __instance.config.dollyRateKeys;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }

                if (RimWorld.KeyBindingDefOf.MapDolly_Down.IsDown) {
                    __instance.desiredRotation.y = -__instance.config.dollyRateKeys;
                    RimWorld.PlayerKnowledgeDatabase.KnowledgeDemonstrated(RimWorld.ConceptDefOf.WorldCameraMovement, RimWorld.KnowledgeAmount.SpecificInteraction);
                }
            }
        }

        [HarmonyPatch]
        static class WorldCameraDriver_Update {
            public static bool Prepare() => TargetMethod() != null;

            public static MethodBase TargetMethod() => AccessTools.Method("RimWorld.Planet.WorldCameraDriver:Update");

            public static bool Prefix(ref RimWorld.Planet.WorldCameraDriver __instance) {
                if (LongEventHandler.ShouldWaitForEvent)
                    return false;
                if (Find.World == null) {
                    __instance.MyCamera.gameObject.SetActive(false);
                } else {
                    if (!Find.WorldInterface.everReset)
                        Find.WorldInterface.Reset();
                    Vector2 curInputDollyVect = __instance.CalculateCurInputDollyVect();
                    if (curInputDollyVect != Vector2.zero) {
                        float num = (float) (((double) __instance.altitude - (double) RimWorld.Planet.WorldCameraDriver.MinAltitude) / (CameraInfo.maxAltitude - (double) RimWorld.Planet.WorldCameraDriver.MinAltitude) * 0.850000023841858 + 0.150000005960464);
                        __instance.rotationVelocity = new Vector2(curInputDollyVect.x, curInputDollyVect.y) * num;
                    }
                    if ((!Input.GetMouseButton(2) || SteamDeck.IsSteamDeck && __instance.releasedLeftWhileHoldingMiddle) && __instance.dragTimeStamps.Any()) {
                        __instance.rotationVelocity += CameraDriver.GetExtraVelocityFromReleasingDragButton(__instance.dragTimeStamps, 5f * CameraInfo.dragVelocityMultiplier);
                        __instance.dragTimeStamps.Clear();
                    }
                    if (!__instance.AnythingPreventsCameraMotion) {
                        float num = Time.deltaTime * CameraDriver.HitchReduceFactor;
                        __instance.sphereRotation *= Quaternion.AngleAxis(__instance.rotationVelocity.x * num * __instance.config.rotationSpeedScale, __instance.MyCamera.transform.up);
                        __instance.sphereRotation *= Quaternion.AngleAxis(-__instance.rotationVelocity.y * num * __instance.config.rotationSpeedScale, __instance.MyCamera.transform.right);
                        if (__instance.desiredRotationRaw != Vector2.zero) {
                            __instance.sphereRotation *= Quaternion.AngleAxis(__instance.desiredRotationRaw.x, __instance.MyCamera.transform.up);
                            __instance.sphereRotation *= Quaternion.AngleAxis(-__instance.desiredRotationRaw.y, __instance.MyCamera.transform.right);
                        }
                        __instance.dragTimeStamps.Add(new CameraDriver.DragTimeStamp() {
                            posDelta = __instance.desiredRotationRaw,
                            time = Time.time
                        });
                    }
                    __instance.desiredRotationRaw = Vector2.zero;
                    int num1 = Gen.FixedTimeStepUpdate(ref __instance.fixedTimeStepBuffer, 60f);
                    for (int index = 0; index < num1; ++index) {
                        if (__instance.rotationVelocity != Vector2.zero) {
                            __instance.rotationVelocity *= __instance.config.camRotationDecayFactor;
                            if ((double) __instance.rotationVelocity.magnitude < 0.0500000007450581)
                                __instance.rotationVelocity = Vector2.zero;
                        }
                        if (__instance.config.smoothZoom) {
                            float num2 = Mathf.Lerp(__instance.altitude, __instance.desiredAltitude, 0.05f);
                            __instance.desiredAltitude += (num2 - __instance.altitude) * __instance.config.zoomPreserveFactor;
                            __instance.altitude = num2;
                        } else {
                            float num2 = (float) (((double) __instance.desiredAltitude - (double) __instance.altitude) * 0.400000005960464);
                            __instance.desiredAltitude += __instance.config.zoomPreserveFactor * num2;
                            __instance.altitude += num2;
                        }
                    }
                    __instance.rotationAnimation_lerpFactor += Time.deltaTime * 8f;
                    if (Find.PlaySettings.lockNorthUp) {
                        __instance.RotateSoNorthIsUp(false);
                        __instance.ClampXRotation(ref __instance.sphereRotation);
                    }
                    for (int index = 0; index < num1; ++index)
                        __instance.config.ConfigFixedUpdate_60(ref __instance.rotationVelocity);
                    __instance.ApplyPositionToGameObject();
                }
                return false;
            }
        }
    }
}
