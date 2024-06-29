using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Universum.Functionality {
    public class Mesh {
        private Random _rand;
        public float minElevation;
        public float maxElevation;
        List<Vector3> _vertices = new List<Vector3>();
        List<int> _triangles = new List<int>();
        List<Vector2> _uvs = null;
        List<Color> _colors = new List<Color>();
        private Bounds _bounds = new Bounds();

        public Mesh(Random rand) {
            _rand = rand;
        }

        public UnityEngine.Mesh GetUnityMesh() {
            Vector2[] uvs = null;
            if (_uvs != null) uvs = _uvs.ToArray();

            UnityEngine.Mesh unityMesh = new UnityEngine.Mesh {
                vertices = _vertices.ToArray(),
                triangles = _triangles.ToArray(),
                uv = uvs,
                colors = _colors.ToArray()
            };

            unityMesh.RecalculateBounds();
            unityMesh.RecalculateTangents();
            unityMesh.RecalculateNormals();
            return unityMesh;
        }

        public void Merge(Mesh secondMesh) {
            int incrementValue = _vertices.Count;
            secondMesh._triangles = secondMesh._triangles.Select(x => x + incrementValue).ToList();
            _vertices.AddRange(secondMesh._vertices);
            _triangles.AddRange(secondMesh._triangles);
            _colors.AddRange(secondMesh._colors);

            if (minElevation > secondMesh.minElevation) minElevation = secondMesh.minElevation;
            if (maxElevation < secondMesh.maxElevation) maxElevation = secondMesh.maxElevation;
        }

        private void _CalculateBounds() {
            if (_vertices.Count == 0) {
                _bounds = new Bounds();
                return;
            }

            Vector3 min = _vertices[0];
            Vector3 max = _vertices[0];

            foreach (Vector3 vertex in _vertices) {
                if (vertex.x < min.x) min.x = vertex.x;
                if (vertex.y < min.y) min.y = vertex.y;
                if (vertex.z < min.z) min.z = vertex.z;

                if (vertex.x > max.x) max.x = vertex.x;
                if (vertex.y > max.y) max.y = vertex.y;
                if (vertex.z > max.z) max.z = vertex.z;
            }

            Vector3 center = (min + max) / 2f;
            Vector3 size = max - min;
            _bounds = new Bounds(center, size);
        }

        public void Subdivide(int iterations = 1) {
            for (int i = 0; i < iterations; i++) {
                List<Vector3> newVertices = new List<Vector3>(_vertices);
                List<int> newTriangles = new List<int>();
                
                Dictionary<string, int> edgeMidpoints = new Dictionary<string, int>();

                for (int j = 0; j < _triangles.Count; j += 3) {
                    int i1 = _triangles[j];
                    int i2 = _triangles[j + 1];
                    int i3 = _triangles[j + 2];

                    int m1 = _GetMidpointIndex(i1, i2, newVertices, edgeMidpoints);
                    int m2 = _GetMidpointIndex(i2, i3, newVertices, edgeMidpoints);
                    int m3 = _GetMidpointIndex(i3, i1, newVertices, edgeMidpoints);

                    newTriangles.Add(i1); newTriangles.Add(m1); newTriangles.Add(m3);
                    newTriangles.Add(m1); newTriangles.Add(i2); newTriangles.Add(m2);
                    newTriangles.Add(m3); newTriangles.Add(m2); newTriangles.Add(i3);
                    newTriangles.Add(m1); newTriangles.Add(m2); newTriangles.Add(m3);
                }

                _vertices = newVertices;
                _triangles = newTriangles;
            }
        }

        public void LaplacianSmoothing(float lambda, int iterations) {
            Vector3[] newVertices = new Vector3[_vertices.Count];

            for (int iter = 0; iter < iterations; iter++) {
                for (int i = 0; i < _vertices.Count; i++) {
                    Vector3 sum = Vector3.zero;
                    int count = 0;

                    for (int j = 0; j < _triangles.Count; j += 3) {
                        if (_triangles[j] == i || _triangles[j + 1] == i || _triangles[j + 2] == i) {
                            sum += _vertices[_triangles[j]] + _vertices[_triangles[j + 1]] + _vertices[_triangles[j + 2]];
                            count += 3;
                        }
                    }

                    if (count > 0) {
                        newVertices[i] = _vertices[i] + lambda * (sum / count - _vertices[i]);
                    } else {
                        newVertices[i] = _vertices[i];
                    }
                }
                _vertices = newVertices.ToList();
            }
        }

        public void ApplyNoise(
            int seed,
            List<bool> isMask,
            List<bool> useMask,
            List<float> noiseStrength,
            List<float> noiseRoughness,
            List<int> noiseIterations,
            List<float> noisePersistence,
            List<float> noiseBaseRoughness,
            List<float> noiseMinValue
        ) {
            minElevation = float.MaxValue;
            maxElevation = float.MinValue;

            for (int i = 0; i < _vertices.Count; i++) {
                _vertices[i] = SimplexPerlinNoise.ApplyNoiseLayers(
                    _vertices[i],
                    isMask,
                    useMask,
                    noiseStrength,
                    noiseRoughness,
                    noiseIterations,
                    noisePersistence,
                    noiseBaseRoughness,
                    noiseMinValue,
                    seed
                );

                float dist = Vector3.Distance(_vertices[i], Vector3.zero);
                if (dist < minElevation) minElevation = dist;
                if (dist > maxElevation) maxElevation = dist;
            }
        }

        public void GenerateColors(Color minElevationColor, Color maxElevationColor) {
            Color[] newColors = new Color[_vertices.Count];
            for (int i = 0; i < _vertices.Count; i++) {
                float dist = Vector3.Distance(_vertices[i], Vector3.zero);
                float distNorm = (dist - minElevation) / (maxElevation - minElevation);

                newColors[i] = Color.Lerp(minElevationColor, maxElevationColor, distNorm * 0.9f);
            }

            _colors = newColors.ToList();
        }

        public void GenerateIcoSphere(float radius, int recursionLevel) {
            List<Vector3> newVertices = new List<Vector3>();
            Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();
            // create 12 vertices of a icosahedron
            float t = (1f + Mathf.Sqrt(5f)) / 2f;

            newVertices.Add(new Vector3(-1f, t, 0f).normalized * radius);
            newVertices.Add(new Vector3(1f, t, 0f).normalized * radius);
            newVertices.Add(new Vector3(-1f, -t, 0f).normalized * radius);
            newVertices.Add(new Vector3(1f, -t, 0f).normalized * radius);

            newVertices.Add(new Vector3(0f, -1f, t).normalized * radius);
            newVertices.Add(new Vector3(0f, 1f, t).normalized * radius);
            newVertices.Add(new Vector3(0f, -1f, -t).normalized * radius);
            newVertices.Add(new Vector3(0f, 1f, -t).normalized * radius);

            newVertices.Add(new Vector3(t, 0f, -1f).normalized * radius);
            newVertices.Add(new Vector3(t, 0f, 1f).normalized * radius);
            newVertices.Add(new Vector3(-t, 0f, -1f).normalized * radius);
            newVertices.Add(new Vector3(-t, 0f, 1f).normalized * radius);
            // create 20 triangles of the icosahedron
            List<Vector3Int> faces = new List<Vector3Int> {
                // 5 faces around point 0
                new Vector3Int(0, 11, 5),
                new Vector3Int(0, 5, 1),
                new Vector3Int(0, 1, 7),
                new Vector3Int(0, 7, 10),
                new Vector3Int(0, 10, 11),
                // 5 adjacent faces 
                new Vector3Int(1, 5, 9),
                new Vector3Int(5, 11, 4),
                new Vector3Int(11, 10, 2),
                new Vector3Int(10, 7, 6),
                new Vector3Int(7, 1, 8),
                // 5 faces around point 3
                new Vector3Int(3, 9, 4),
                new Vector3Int(3, 4, 2),
                new Vector3Int(3, 2, 6),
                new Vector3Int(3, 6, 8),
                new Vector3Int(3, 8, 9),
                // 5 adjacent faces 
                new Vector3Int(4, 9, 5),
                new Vector3Int(2, 4, 11),
                new Vector3Int(6, 2, 10),
                new Vector3Int(8, 6, 7),
                new Vector3Int(9, 8, 1)
            };
            // refine triangles
            for (int i = 0; i < recursionLevel; i++) {
                List<Vector3Int> faces2 = new List<Vector3Int>();
                foreach (var tri in faces) {
                    // replace triangle by 4 triangles
                    int a = _GetMiddlePoint(tri.x, tri.y, ref newVertices, ref middlePointIndexCache, radius);
                    int b = _GetMiddlePoint(tri.y, tri.z, ref newVertices, ref middlePointIndexCache, radius);
                    int c = _GetMiddlePoint(tri.z, tri.x, ref newVertices, ref middlePointIndexCache, radius);

                    faces2.Add(new Vector3Int(tri.x, a, c));
                    faces2.Add(new Vector3Int(tri.y, b, a));
                    faces2.Add(new Vector3Int(tri.z, c, b));
                    faces2.Add(new Vector3Int(a, b, c));
                }
                faces = faces2;
            }

            List<int> newTriangles = new List<int>();
            for (int i = 0; i < faces.Count; i++) {
                newTriangles.Add(faces[i].x);
                newTriangles.Add(faces[i].y);
                newTriangles.Add(faces[i].z);
            }

            _vertices = newVertices;
            _triangles = newTriangles;
        }

        public void GenerateQuadSphere(float radius, int detail) {
            int verticesPerFace = (detail + 1) * (detail + 1);
            int totalVertices = 6 * verticesPerFace;

            int trianglesPerFace = detail * detail * 6;
            int totalTriangles = 6 * trianglesPerFace;

            Vector3[] newVertices = new Vector3[totalVertices];
            int[] newTriangles = new int[totalTriangles];

            Vector3[] faceNormals = { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.left, Vector3.right };

            int v = 0, t = 0;

            for (int f = 0; f < 6; f++) {
                Vector3 normal = faceNormals[f];
                Vector3 right, up;

                if (normal == Vector3.up) {
                    right = Vector3.right;
                    up = Vector3.forward;
                } else if (normal == Vector3.down) {
                    right = Vector3.right;
                    up = Vector3.back;
                } else {
                    right = Vector3.Cross(normal, Vector3.up);
                    up = Vector3.Cross(right, normal);
                }

                for (int i = 0; i <= detail; i++) {
                    for (int j = 0; j <= detail; j++) {
                        float x = (j * 2f / detail) - 1f;
                        float y = (i * 2f / detail) - 1f;

                        Vector3 vertex = normal + right * x + up * y;
                        newVertices[v] = vertex.normalized * radius;
                        v++;

                        if (i < detail && j < detail) {
                            int topLeft = v - 1;
                            int topRight = topLeft + 1;
                            int bottomLeft = topLeft + detail + 1;
                            int bottomRight = bottomLeft + 1;

                            newTriangles[t++] = topLeft;
                            newTriangles[t++] = bottomLeft;
                            newTriangles[t++] = bottomRight;
                            newTriangles[t++] = topLeft;
                            newTriangles[t++] = bottomRight;
                            newTriangles[t++] = topRight;
                        }
                    }
                }
            }

            _vertices = newVertices.ToList();
            _triangles = newTriangles.ToList();
        }

        public void GenerateBox(Vector3 dimensions, int detail) {
            int verticesPerFace = (detail + 1) * (detail + 1);
            int totalVertices = 6 * verticesPerFace;
            Vector3[] newVertices = new Vector3[totalVertices];

            int trianglesPerFace = detail * detail * 6;
            int totalTriangles = 6 * trianglesPerFace;
            int[] newTriangles = new int[totalTriangles];

            Vector3[] faceNormals = { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.left, Vector3.right };

            Vector3[] faceSizes = {
                new Vector3(dimensions.x, dimensions.y, dimensions.z),
                new Vector3(dimensions.x, dimensions.y, dimensions.z),
                new Vector3(dimensions.x, dimensions.z, dimensions.y),
                new Vector3(dimensions.x, dimensions.z, dimensions.y),
                new Vector3(dimensions.z, dimensions.y, dimensions.x),
                new Vector3(dimensions.z, dimensions.y, dimensions.x)
            };

            int v = 0, t = 0;

            for (int f = 0; f < 6; f++) {
                Vector3 normal = faceNormals[f];
                Vector3 size = faceSizes[f];
                Vector3 right, up;
                if (normal == Vector3.up) {
                    right = Vector3.right;
                    up = Vector3.forward;
                } else if (normal == Vector3.down) {
                    right = Vector3.right;
                    up = Vector3.back;
                } else {
                    right = Vector3.Cross(normal, Vector3.up);
                    up = Vector3.Cross(right, normal);
                }

                for (int i = 0; i <= detail; i++) {
                    for (int j = 0; j <= detail; j++) {
                        float x = j * size.x / detail - size.x / 2;
                        float y = i * size.y / detail - size.y / 2;
                        _vertices[v] = normal * size.z / 2 + right * x + up * y;
                        v++;

                        if (i < detail && j < detail) {
                            int topLeft = v - 1;
                            int topRight = topLeft + 1;
                            int bottomLeft = topLeft + detail + 1;
                            int bottomRight = bottomLeft + 1;

                            newTriangles[t++] = topLeft;
                            newTriangles[t++] = bottomRight;
                            newTriangles[t++] = topRight;
                            newTriangles[t++] = topLeft;
                            newTriangles[t++] = bottomLeft;
                            newTriangles[t++] = bottomRight;
                        }
                    }
                }
            }

            _vertices = newVertices.ToList();
            _triangles = newTriangles.ToList();
        }

        public void GenerateTorus(float radius, float tubeRadius, float thickness, int radialSegments, int tubularSegments, Color color) {
            List<Vector3> vertices = new List<Vector3>();
            List<int> triangles = new List<int>();
            List<Vector2> uvs = new List<Vector2>();
            List<Color> colors = new List<Color>();

            float radialStep = 2 * Mathf.PI / radialSegments;
            float tubularStep = 2 * Mathf.PI / tubularSegments;

            for (int i = 0; i <= radialSegments; i++) {
                float u = i * radialStep;
                float cu = Mathf.Cos(u);
                float su = Mathf.Sin(u);

                for (int j = 0; j <= tubularSegments; j++) {
                    float v = j * tubularStep;
                    float cv = Mathf.Cos(v);
                    float sv = Mathf.Sin(v);

                    Vector3 vertex = new Vector3(
                        (radius + tubeRadius * cv) * cu,
                        sv * thickness,
                        (radius + tubeRadius * cv) * su
                    );

                    vertices.Add(vertex);
                    uvs.Add(new Vector2((float) i / radialSegments, (float) j / tubularSegments));

                    float normalizedJ = (float) j / tubularSegments;
                    float alpha = Mathf.Pow(0.3f * Mathf.Cos(normalizedJ * Mathf.PI), 2);
                    colors.Add(new Color(color.r, color.g, color.b, alpha));
                }
            }

            for (int j = 1; j <= radialSegments; j++) {
                for (int i = 1; i <= tubularSegments; i++) {
                    int a = (tubularSegments + 1) * j + i - 1;
                    int b = (tubularSegments + 1) * (j - 1) + i - 1;
                    int c = (tubularSegments + 1) * (j - 1) + i;
                    int d = (tubularSegments + 1) * j + i;

                    triangles.Add(a);
                    triangles.Add(d);
                    triangles.Add(b);
                    triangles.Add(b);
                    triangles.Add(d);
                    triangles.Add(c);
                }
            }

            _vertices = vertices;
            _triangles = triangles;
            _uvs = uvs;
            _colors = colors;
        }

        private int _GetMidpointIndex(int i1, int i2, List<Vector3> vertices, Dictionary<string, int> edgeMidpoints) {
            string edgeKey = i1 < i2 ? i1 + "_" + i2 : i2 + "_" + i1;

            if (edgeMidpoints.ContainsKey(edgeKey)) {
                return edgeMidpoints[edgeKey];
            }

            Vector3 v1 = vertices[i1];
            Vector3 v2 = vertices[i2];
            Vector3 midpoint = (v1 + v2) / 2;

            vertices.Add(midpoint);
            int midpointIndex = vertices.Count - 1;
            edgeMidpoints.Add(edgeKey, midpointIndex);

            return midpointIndex;
        }

        /*
         * Return index of point in the middle of p1 and p2.
         */
        private int _GetMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache, float radius) {
            // first check if we have it already
            bool firstIsSmaller = p1 < p2;
            long smallerIndex = firstIsSmaller ? p1 : p2;
            long greaterIndex = firstIsSmaller ? p2 : p1;
            long key = (smallerIndex << 32) + greaterIndex;

            if (cache.TryGetValue(key, out int ret)) {
                return ret;
            }
            // not in cache, calculate it
            Vector3 point1 = vertices[p1];
            Vector3 point2 = vertices[p2];
            Vector3 middle = new Vector3((point1.x + point2.x) / 2f, (point1.y + point2.y) / 2f, (point1.z + point2.z) / 2f);
            // add vertex makes sure point is on unit sphere
            int i = vertices.Count;
            vertices.Add(middle.normalized * radius);
            // store it, return index
            cache.Add(key, i);

            return i;
        }

        public void ApplyVoronoiPattern(int siteCount, float craterDepth, float craterRimHeight, Color craterBottomColor, Color craterRimColor) {
            _CalculateBounds();

            Vector3 asteroidCenter = _bounds.center;

            // generate random seed points
            List<Vector3> seeds = new List<Vector3>();
            for (int i = 0; i < siteCount; i++) {
                Vector3 seed = new Vector3(
                    _bounds.min.x + _rand.GetFloat() * _bounds.size.x,
                    _bounds.min.y + _rand.GetFloat() * _bounds.size.y,
                    _bounds.min.z + _rand.GetFloat() * _bounds.size.z
                );
                seeds.Add(seed);
            }

            Color[] newColors = new Color[_vertices.Count];
            float maxDistanceFromSeed = _bounds.size.magnitude / 2;

            // displace vertices and color them
            for (int i = 0; i < _vertices.Count; i++) {
                Vector3 vertex = _vertices[i];

                // find the nearest seed
                float minDistance = float.MaxValue;
                for (int j = 0; j < seeds.Count; j++) {
                    float distance = Vector3.Distance(vertex, seeds[j]);
                    if (distance < minDistance) {
                        minDistance = distance;
                    }
                }

                // direction from the center of the asteroid to the vertex
                Vector3 directionFromCenter = (vertex - asteroidCenter).normalized;

                // calculate displacement to create the crater effect
                float normalizedDistance = minDistance / _bounds.size.magnitude;
                float displacement = -craterDepth + (craterDepth + craterRimHeight) * normalizedDistance;

                vertex += directionFromCenter * displacement;

                // color interpolation based on distance from the nearest seed
                float colorFactor = Mathf.Clamp01(minDistance / maxDistanceFromSeed);
                newColors[i] = Color.Lerp(craterBottomColor, craterRimColor, colorFactor);

                _vertices[i] = vertex;
            }

            _colors = newColors.ToList();
        }
    }
}
