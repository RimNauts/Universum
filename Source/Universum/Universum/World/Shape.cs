using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Universum.World {
    public class Shape {
        readonly Functionality.Random _rand;

        public float highestElevation;
        List<Functionality.Mesh> _meshes = new List<Functionality.Mesh>();
        List<Material> _materials = new List<Material>();

        public Shape(Functionality.Random rand) {
            _rand = rand;
        }

        public void CompressData() {
            if (_meshes.Count <= 1) return;

            Dictionary<Material, Functionality.Mesh> meshMaterialMap = new Dictionary<Material, Functionality.Mesh>();
            for (int i = 0; i < _meshes.Count; i++) {
                if (meshMaterialMap.TryGetValue(_materials[i], out var mesh)) {
                    mesh.Merge(_meshes[i]);
                } else meshMaterialMap[_materials[i]] = _meshes[i];
            }

            _materials = meshMaterialMap.Keys.ToList();
            _meshes = meshMaterialMap.Values.ToList();
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
            Color? minElevationColor,
            Color? maxElevationColor,
            float craterDepth,
            float craterRimHeight,
            List<bool> isMask,
            List<bool> useMask,
            List<float> noiseStrength,
            List<float> noiseRoughness,
            List<int> noiseIterations,
            List<float> noisePersistence,
            List<float> noiseBaseRoughness,
            List<float> noiseMinValue
        ) {
            Functionality.Mesh mesh = new Functionality.Mesh(_rand);
            switch (type) {
                case Defs.ShapeType.PREV:
                    mesh = _meshes[_meshes.Count - 1];
                    mesh.Subdivide(subdivisionIterations);
                    _materials[_materials.Count - 1] = material;
                    break;
                case Defs.ShapeType.SPHERE:
                    mesh.GenerateIcoSphere(radius, detail);
                    _meshes.Add(mesh);
                    _materials.Add(material);
                    break;
                case Defs.ShapeType.QUADSPHERE:
                    mesh.GenerateQuadSphere(radius, detail);
                    _meshes.Add(mesh);
                    _materials.Add(material);
                    break;
                case Defs.ShapeType.BOX:
                    mesh.GenerateBox(dimensions, detail);
                    _meshes.Add(mesh);
                    _materials.Add(material);
                    break;
                case Defs.ShapeType.VORONOI:
                    mesh = _meshes[_meshes.Count - 1];
                    mesh.ApplyVoronoiPattern(siteCount: detail, craterDepth, craterRimHeight, (Color) minElevationColor, (Color) maxElevationColor);
                    _materials[_materials.Count - 1] = material;
                    break;
                default:
                    mesh.GenerateIcoSphere(radius, detail);
                    _meshes.Add(mesh);
                    _materials.Add(material);
                    break;
            }

            mesh.ApplyNoise(
                _rand.seed,
                isMask,
                useMask,
                noiseStrength,
                noiseRoughness,
                noiseIterations,
                noisePersistence,
                noiseBaseRoughness,
                noiseMinValue
            );

            if (minElevationColor != null && maxElevationColor != null && type != Defs.ShapeType.VORONOI) {
                mesh.GenerateColors((Color) minElevationColor, (Color) maxElevationColor);
            }

            if (highestElevation < mesh.maxElevation) highestElevation = mesh.maxElevation;
        }
    }
}
