using System.Collections.Generic;
using UnityEngine;

namespace Universum.Functionality {
    public static class IcoSphere {
        public static Mesh Create(float radius, int recursionLevel) {
            Mesh mesh = new Mesh();
            Vector3[] vertices = mesh.vertices;
            List<Vector3> vertList = new List<Vector3>();
            Dictionary<long, int> middlePointIndexCache = new Dictionary<long, int>();
            // create 12 vertices of a icosahedron
            float t = (1f + Mathf.Sqrt(5f)) / 2f;

            vertList.Add(new Vector3(-1f, t, 0f).normalized * radius);
            vertList.Add(new Vector3(1f, t, 0f).normalized * radius);
            vertList.Add(new Vector3(-1f, -t, 0f).normalized * radius);
            vertList.Add(new Vector3(1f, -t, 0f).normalized * radius);

            vertList.Add(new Vector3(0f, -1f, t).normalized * radius);
            vertList.Add(new Vector3(0f, 1f, t).normalized * radius);
            vertList.Add(new Vector3(0f, -1f, -t).normalized * radius);
            vertList.Add(new Vector3(0f, 1f, -t).normalized * radius);

            vertList.Add(new Vector3(t, 0f, -1f).normalized * radius);
            vertList.Add(new Vector3(t, 0f, 1f).normalized * radius);
            vertList.Add(new Vector3(-t, 0f, -1f).normalized * radius);
            vertList.Add(new Vector3(-t, 0f, 1f).normalized * radius);
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
                    int a = GetMiddlePoint(tri.x, tri.y, ref vertList, ref middlePointIndexCache, radius);
                    int b = GetMiddlePoint(tri.y, tri.z, ref vertList, ref middlePointIndexCache, radius);
                    int c = GetMiddlePoint(tri.z, tri.x, ref vertList, ref middlePointIndexCache, radius);

                    faces2.Add(new Vector3Int(tri.x, a, c));
                    faces2.Add(new Vector3Int(tri.y, b, a));
                    faces2.Add(new Vector3Int(tri.z, c, b));
                    faces2.Add(new Vector3Int(a, b, c));
                }
                faces = faces2;
            }

            List<int> triList = new List<int>();
            for (int i = 0; i < faces.Count; i++) {
                triList.Add(faces[i].x);
                triList.Add(faces[i].y);
                triList.Add(faces[i].z);
            }

            mesh.vertices = vertList.ToArray();
            mesh.triangles = triList.ToArray();
            mesh.uv = new Vector2[vertices.Length];
            mesh.normals = new Vector3[vertList.Count];

            return mesh;
        }

        /*
         * Return index of point in the middle of p1 and p2.
         */
        private static int GetMiddlePoint(int p1, int p2, ref List<Vector3> vertices, ref Dictionary<long, int> cache, float radius) {
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
    }
}
