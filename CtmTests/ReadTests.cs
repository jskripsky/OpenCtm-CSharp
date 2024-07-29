using System;
using System.IO;
using NUnit.Framework;
using OpenCTM;

namespace CtmTests
{
	[TestFixture]
	public class ReadTests
	{
		private static float[] vert = new float[]{	0,0,0,
													1,0,0,
													1,1,0,
													0,1,0};
		private static float[] normals = new float[]{1,1,1,
													1,1,1,
													1,1,1,
													1,1,1};
		private static AttributeData[] uv = { new AttributeData ("uv1", "test",
			                                                        AttributeData.STANDARD_UV_PRECISION,
			                                                        new float[]{0.0034f, 0.1f,
																				0.8f, 1.0f,
																				0.351f, 0.612f,
																				0.1229f, 0.91224f}) };
		private static int[] ind = new int[]{0,1,2,0,2,3};
		private static Mesh quad = new Mesh (vert, normals, ind, uv, new AttributeData[0]);

		[Test]
		public void rawTest ()
		{
			testEncoder (new RawEncoder ());
		}

		[Test]
		public void mg1Test ()
		{
			testEncoder (new MG1Encoder ());
		}

		[Test]
		public void mg2Test ()
		{
			testEncoder (new MG2Encoder (1 / 1024f, 1 / 1024f));
		}

		[Test]
		public void readTest ()
		{
			FileStream file = new FileStream ("resources/brunnen.ctm", FileMode.Open);
			CtmFileReader reader = new CtmFileReader (file);

			Mesh m = reader.decode ();

			m.checkIntegrity ();
		}

		private void testEncoder (MeshEncoder encoder)
		{
			MemoryStream memory = new MemoryStream ();
			CtmFileWriter writer = new CtmFileWriter (memory, encoder);
			writer.encode (quad, null);

			memory.Seek (0, SeekOrigin.Begin);
			Stream readMemory = new MemoryStream (memory.ToArray ());
			CtmFileReader reader = new CtmFileReader (readMemory);
			Mesh m = reader.decode ();

			m.checkIntegrity ();

			MG2Encoder mg2 = encoder as MG2Encoder;

			if (mg2 == null)
				Assert.Equals (quad, m);
			else {
				MG2MeshEqualsTest (mg2, quad, m);
			}
		}

		private void MG2MeshEqualsTest (MG2Encoder enc, Mesh orig, Mesh read)
		{
			//Assert.Equals (orig.getTriangleCount (), read.getTriangleCount (), "Trianglecount differs");
			//Assert.Equals (orig.getVertexCount (), read.getVertexCount (), "Vertexcount differs");
			//Assert.Equals (orig.hasNormals (), read.hasNormals (), "Only one has normals");

			Grid grid = enc.setupGrid (orig.vertices);
			SortableVertex[] sorted = enc.sortVertices (grid, orig.vertices);
			int[] indexLUT = new int[sorted.Length];
			for (int i = 0; i < sorted.Length; ++i) {
				indexLUT [sorted [i].originalIndex] = i;
			}

			for (int i=0; i < orig.getVertexCount(); i++) {
				int newIndex = indexLUT [i];

				for (int e = 0; e < Mesh.CTM_POSITION_ELEMENT_COUNT; e++) {
					Assert.That (compare (orig.vertices [i * 3 + e], read.vertices [newIndex * 3 + e], enc.vertexPrecision * 2),
					               "positions not in precision");
				}
				if (orig.hasNormals ()) {
					for (int e = 0; e < Mesh.CTM_NORMAL_ELEMENT_COUNT; e++) {
						Assert.That (compare (orig.normals [i * 3 + e], read.normals [newIndex * 3 + e], enc.normalPrecision * 10),
						               "normals not in precision");
					}
				}
			}

			testAttributeArrays (orig.texcoordinates, read.texcoordinates, indexLUT);
			testAttributeArrays (orig.attributs, read.attributs, indexLUT);
		}

		private void testAttributeArrays (AttributeData[] a, AttributeData[] b, int[] indexLUT)
		{
			if ((a == null || a.Length == 0) && (b == null || b.Length == 0))
				return;

			Assert.Equals (a.Length, b.Length);

			for (int i=0; i < a.Length; i++) {
				Assert.Equals (a [i].materialName, b [i].materialName);
				Assert.Equals (a [i].name, b [i].name);
				Assert.Equals (a [i].precision, b [i].precision);

				float[] orig = a [i].values;
				float[] read = b [i].values;

				Assert.Equals (orig.Length, read.Length);

				int count = orig.Length / indexLUT.Length;

				Assert.Equals (count * indexLUT.Length, orig.Length);


				for (int vi=0; vi < indexLUT.Length; vi++) {
					int newIndex = indexLUT [vi];

					for (int e = 0; e < count; e++) {
						Assert.That (compare (orig [vi * count + e], read [newIndex * count + e], a [i].precision * 2),
						               "Attributs not in precision");
					}
				}

			}
		}

		private bool compare (float a, float b, float precision)
		{
			return Math.Abs (a - b) < precision;
		}
	}
}

