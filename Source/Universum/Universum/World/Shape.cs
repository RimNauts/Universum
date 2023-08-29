using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Universum.World {
    public class Shape {
        public List<Functionality.Mesh> meshes = new List<Functionality.Mesh>();
        public List<Material> materials = new List<Material>();
        public float highestElevation;
        readonly int _seed;
        int _totalMeshes = 0;
        UnityEngine.Mesh[] _meshes = new UnityEngine.Mesh[0];
        Material[] _materials = new Material[0];

        public Shape(int seed) {
            _seed = seed;
        }

        public void Add(
            Material material,
            Defs.ShapeType type,
            int subdivisionIterations,
            int detail,
            float radius,
            Vector3 dimensions,
            Color minElevationColor,
            Color maxElevationColor,
            List<bool> isMask,
            List<bool> useMask,
            List<float> noiseStrength,
            List<float> noiseRoughness,
            List<int> noiseIterations,
            List<float> noisePersistence,
            List<float> noiseBaseRoughness,
            List<float> noiseMinValue
        ) {
            Functionality.Mesh mesh = new Functionality.Mesh();
            switch (type) {
                case Defs.ShapeType.SPHERE:
                    mesh.GenerateIcoSphere(radius, detail);
                    meshes.Add(mesh);
                    materials.Add(material);
                    break;
                case Defs.ShapeType.BOX:
                    mesh.GenerateBox(dimensions, detail);
                    meshes.Add(mesh);
                    materials.Add(material);
                    break;
                case Defs.ShapeType.PREV:
                    mesh = meshes[meshes.Count - 1];
                    mesh.Subdivide(subdivisionIterations);
                    materials[materials.Count - 1] = material;
                    break;
                default:
                    mesh.GenerateIcoSphere(radius, detail);
                    meshes.Add(mesh);
                    materials.Add(material);
                    break;
            }

            mesh.ApplyNoise(
                _seed,
                isMask,
                useMask,
                noiseStrength,
                noiseRoughness,
                noiseIterations,
                noisePersistence,
                noiseBaseRoughness,
                noiseMinValue
            );

            mesh.GenerateColors(minElevationColor, maxElevationColor);

            Recache();
        }

        public void Render(Matrix4x4 transformationMatrix) {
            for (int i = 0; i < _totalMeshes; i++) {
                Graphics.Internal_DrawMesh(
                    _meshes[i],
                    submeshIndex: 0,
                    transformationMatrix,
                    _materials[i],
                    RimWorld.Planet.WorldCameraManager.WorldLayer,
                    camera: null,
                    properties: null,
                    ShadowCastingMode.On,
                    receiveShadows: true,
                    probeAnchor: null,
                    lightProbeUsage: LightProbeUsage.BlendProbes,
                    lightProbeProxyVolume: null
                );
            }
        }

        public void Recache() {
            _totalMeshes = meshes.Count;

            _meshes = new Mesh[_totalMeshes];
            _materials = new Material[_totalMeshes];

            for (int i = 0; i < _totalMeshes; i++) {
                _meshes[i] = meshes[i].GetUnityMesh();
                _materials[i] = materials[i];
            }
        }
    }
}
