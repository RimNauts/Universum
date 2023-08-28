using UnityEngine;

namespace Universum.Functionality {
    public static class LaplacianSmoothing {
        public static void Smooth(Mesh mesh, float lambda, int iterations) {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            Vector3[] newVertices = new Vector3[vertices.Length];

            for (int iter = 0; iter < iterations; iter++) {
                for (int i = 0; i < vertices.Length; i++) {
                    Vector3 sum = Vector3.zero;
                    int count = 0;

                    for (int j = 0; j < triangles.Length; j += 3) {
                        if (triangles[j] == i || triangles[j + 1] == i || triangles[j + 2] == i) {
                            sum += vertices[triangles[j]] + vertices[triangles[j + 1]] + vertices[triangles[j + 2]];
                            count += 3;
                        }
                    }

                    if (count > 0) {
                        newVertices[i] = vertices[i] + lambda * (sum / count - vertices[i]);
                    } else {
                        newVertices[i] = vertices[i];
                    }
                }

                vertices = (Vector3[]) newVertices.Clone();
            }

            mesh.vertices = newVertices;
            mesh.RecalculateNormals();
        }
    }
}
