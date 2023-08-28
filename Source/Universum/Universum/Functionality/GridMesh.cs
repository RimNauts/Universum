using UnityEngine;

namespace Universum.Functionality {
    public static class GridMesh {
        public static Mesh Create(Vector3 dimensions, int detail) {
            Mesh mesh = new Mesh();

            int verticesPerFace = (detail + 1) * (detail + 1);
            int totalVertices = 6 * verticesPerFace;
            Vector3[] vertices = new Vector3[totalVertices];

            int trianglesPerFace = detail * detail * 6;
            int totalTriangles = 6 * trianglesPerFace;
            int[] triangles = new int[totalTriangles];

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
                        vertices[v] = normal * size.z / 2 + right * x + up * y;
                        v++;

                        if (i < detail && j < detail) {
                            int topLeft = v - 1;
                            int topRight = topLeft + 1;
                            int bottomLeft = topLeft + detail + 1;
                            int bottomRight = bottomLeft + 1;

                            triangles[t++] = topLeft;
                            triangles[t++] = bottomRight;
                            triangles[t++] = topRight;
                            triangles[t++] = topLeft;
                            triangles[t++] = bottomLeft;
                            triangles[t++] = bottomRight;
                        }
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}
