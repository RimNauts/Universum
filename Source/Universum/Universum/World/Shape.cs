using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using Universum.Functionality;

namespace Universum.World {
    public class Shape {
        public int icoSphereRecursionLevel;
        public List<Mesh> meshes = new List<Mesh>();
        public List<Material> materials = new List<Material>();
        readonly int _seed;
        int _totalMeshes = 0;
        Mesh[] _meshes = new Mesh[0];
        Material[] _materials = new Material[0];

        public Shape(int seed, int icoSphereRecursionLevel) {
            _seed = seed;
            this.icoSphereRecursionLevel = icoSphereRecursionLevel;
        }

        public void Add(
            Material material,
            float radius,
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
            Mesh baseMesh = new Mesh();
            IcoSphere.Create(baseMesh, icoSphereRecursionLevel, radius);

            Mesh mesh = _CloneMesh(baseMesh);

            List<Vector3> vertices = mesh.vertices.ToList();
            Color[] colors = new Color[mesh.vertices.Length];

            float minElevation = float.MaxValue;
            float MaxElevation = float.MinValue;

            for (int i = 0; i < vertices.Count; i++) {
                vertices[i] = SimplexPerlinNoise.ApplyNoiseLayers(
                    vertices[i],
                    isMask,
                    useMask,
                    noiseStrength,
                    noiseRoughness,
                    noiseIterations,
                    noisePersistence,
                    noiseBaseRoughness,
                    noiseMinValue,
                    _seed
                );

                float dist = Vector3.Distance(vertices[i], Vector3.zero);
                if (dist < minElevation) minElevation = dist;
                if (dist > MaxElevation) MaxElevation = dist;
            }

            for (int i = 0; i < vertices.Count; i++) {
                float dist = Vector3.Distance(vertices[i], Vector3.zero);
                float distNorm = (dist - minElevation) / (MaxElevation - minElevation);

                colors[i] = Color.Lerp(minElevationColor, maxElevationColor, distNorm * 0.9f);
            }

            mesh.vertices = vertices.ToArray();
            mesh.colors = colors;

            meshes.Add(mesh);
            materials.Add(material);

            _Recache();
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

        private void _Recache() {
            _totalMeshes = meshes.Count;

            _meshes = new Mesh[_totalMeshes];
            _materials = new Material[_totalMeshes];

            for (int i = 0; i < _totalMeshes; i++) {
                meshes[i].RecalculateBounds();
                meshes[i].RecalculateTangents();
                meshes[i].RecalculateNormals();

                _meshes[i] = meshes[i];
                _materials[i] = materials[i];
            }
        }

        private Mesh _CloneMesh(Mesh mesh) {
            Mesh newMesh = new Mesh();
            newMesh.Clear();

            newMesh.vertices = (Vector3[]) mesh.vertices.Clone();
            newMesh.triangles = (int[]) mesh.triangles.Clone();
            newMesh.uv = (Vector2[]) mesh.uv.Clone();
            newMesh.colors = new Color[mesh.vertices.Length];

            return newMesh;
        }
    }
}
