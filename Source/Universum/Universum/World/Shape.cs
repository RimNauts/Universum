using System.Collections.Generic;
using UnityEngine;

namespace Universum.World {
    public class Shape {
        public float highestElevation;
        readonly List<Functionality.Mesh> _meshes = new List<Functionality.Mesh>();
        readonly List<Material> _materials = new List<Material>();
        readonly int _seed;

        public Shape(int seed) {
            _seed = seed;
        }

        public Mesh[] GetMeshes() {
            Mesh[] meshes = new Mesh[_meshes.Count];
            for (int i = 0; i < meshes.Length; i++) meshes[i] = _meshes[i].GetUnityMesh();
            return meshes;
        }

        public Material[] GetMaterials() {
            return _materials.ToArray();
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
                    _meshes.Add(mesh);
                    _materials.Add(material);
                    break;
                case Defs.ShapeType.BOX:
                    mesh.GenerateBox(dimensions, detail);
                    _meshes.Add(mesh);
                    _materials.Add(material);
                    break;
                case Defs.ShapeType.PREV:
                    mesh = _meshes[_meshes.Count - 1];
                    mesh.Subdivide(subdivisionIterations);
                    _materials[_materials.Count - 1] = material;
                    break;
                default:
                    mesh.GenerateIcoSphere(radius, detail);
                    _meshes.Add(mesh);
                    _materials.Add(material);
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

            if (highestElevation < mesh.maxElevation) highestElevation = mesh.maxElevation;
        }
    }
}
