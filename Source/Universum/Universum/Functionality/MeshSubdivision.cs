using System.Collections.Generic;
using UnityEngine;

namespace Universum.Functionality {
    public static class MeshSubdivision {
        public static void Subdivide(Mesh mesh) {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            // Dictionary to store midpoints
            Dictionary<string, int> edgeMidpoints = new Dictionary<string, int>();
            List<Vector3> newVertices = new List<Vector3>(vertices);
            List<int> newTriangles = new List<int>();

            for (int i = 0; i < triangles.Length; i += 3) {
                int i1 = triangles[i];
                int i2 = triangles[i + 1];
                int i3 = triangles[i + 2];

                // Calculate midpoints
                int m1 = _GetMidpointIndex(i1, i2, newVertices, edgeMidpoints);
                int m2 = _GetMidpointIndex(i2, i3, newVertices, edgeMidpoints);
                int m3 = _GetMidpointIndex(i3, i1, newVertices, edgeMidpoints);

                // Create new triangles
                newTriangles.Add(i1); newTriangles.Add(m1); newTriangles.Add(m3);
                newTriangles.Add(m1); newTriangles.Add(i2); newTriangles.Add(m2);
                newTriangles.Add(m3); newTriangles.Add(m2); newTriangles.Add(i3);
                newTriangles.Add(m1); newTriangles.Add(m2); newTriangles.Add(m3);
            }

            mesh.vertices = newVertices.ToArray();
            mesh.triangles = newTriangles.ToArray();
            mesh.RecalculateNormals();
        }

        private static int _GetMidpointIndex(int i1, int i2, List<Vector3> vertices, Dictionary<string, int> edgeMidpoints) {
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
    }
}
