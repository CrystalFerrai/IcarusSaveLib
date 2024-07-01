// Copyright 2023 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UeSaveGame;
using UeSaveGame.PropertyTypes;
using UeSaveGame.Util;

namespace IcarusSaveLib
{
	/// <summary>
	/// Helpers for serialization of prospect data
	/// </summary>
	public static class ProspectSerlializationUtil
	{
		public static readonly PackageVersion IcarusPackageVersion;

		static ProspectSerlializationUtil()
		{
			IcarusPackageVersion = new PackageVersion()
			{
				PackageVersionUE4 = EObjectUE4Version.VER_UE4_CORRECT_LICENSEE_FLAG,
				PackageVersionUE5 = EObjectUE5Version.INVALID
			};
		}

		/// <summary>
		/// Deserializes the data associated with an Icarus recorder component into a list of properties
		/// </summary>
		/// <param name="recorderData">The BinaryData property of the recorder component</param>
		/// <exception cref="ArgumentNullException">A parameter is null</exception>
		public static IList<UProperty> DeserializeRecorderData(UProperty recorderData)
		{
			if (recorderData.Value == null) throw new ArgumentNullException(nameof(recorderData));

			ArrayProperty dataProp = (ArrayProperty)recorderData;

			byte[] recorderBytes = new byte[dataProp.Value!.Length];
			for (int i = 0; i < dataProp.Value.Length; ++i)
			{
				recorderBytes[i] = ((byte[])dataProp.Value!)[i]!;
			}

			using (MemoryStream mem = new(recorderBytes))
			using (BinaryReader reader = new(mem))
			{
				return new List<UProperty>(PropertySerializationHelper.ReadProperties(reader, IcarusPackageVersion, true));
			}
		}

		/// <summary>
		/// Serializes the data associated with an Icarus recorder component into a BinaryData property
		/// </summary>
		/// <param name="properties">The properties of the recorder</param>
		public static UProperty SerializeRecorderData(IEnumerable<UProperty> properties)
		{
			byte[] recorderBytes;
			using (MemoryStream mem = new())
			using (BinaryWriter writer = new(mem))
			{
				PropertySerializationHelper.WriteProperties(properties, writer, IcarusPackageVersion, true);
				recorderBytes = mem.ToArray();
			}

			return new ArrayProperty(new FString("BinaryData"))
			{
				Value = recorderBytes
			};
		}
	}
}
