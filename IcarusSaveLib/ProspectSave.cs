// Copyright 2025 Crystal Ferrai
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

using Ionic.Zlib;
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;
using UeSaveGame;
using UeSaveGame.Util;

namespace IcarusSaveLib
{
	/// <summary>
	/// Represents an Icarus prospect save file
	/// </summary>
	public class ProspectSave
	{
#pragma warning disable CS0649 // "field never set" - is set by Json deserializer
		[JsonProperty(PropertyName = nameof(ProspectInfo))]
		private FProspectInfo mProspectInfo;

		[JsonProperty(PropertyName = nameof(ProspectBlob))]
		private FProspectBlob mProspectBlob;
#pragma warning restore CS0649

		private readonly List<FPropertyTag> mProspectData;

		[JsonIgnore]
		public FProspectInfo ProspectInfo
		{
			get => mProspectInfo;
			set => mProspectInfo = value;
		}

		[JsonIgnore]
		public FProspectBlob ProspectBlob => mProspectBlob;

		[JsonIgnore]
		public IList<FPropertyTag> ProspectData => mProspectData;

		public ProspectSave()
		{
			mProspectInfo = new FProspectInfo();
			mProspectBlob = new FProspectBlob();
			mProspectData = new List<FPropertyTag>();
		}

		/// <summary>
		/// Loads a prospect from the given stream
		/// </summary>
		/// <param name="stream">The stream to read from</param>
		public static ProspectSave? Load(Stream stream)
		{
			ProspectSave? instance;

			JsonSerializer serializer = new();
			using (StreamReader sr = new(stream))
			{
				instance = serializer.Deserialize(sr, typeof(ProspectSave)) as ProspectSave;
			}

			if (instance != null)
			{
				byte[] compressed = Convert.FromBase64String(instance.mProspectBlob.BinaryBlob);

				using (MemoryStream mem = new(compressed))
				using (ZlibStream zstream = new(mem, CompressionMode.Decompress))
				using (BinaryReader reader = new(zstream))
				{
					instance.mProspectData.AddRange(PropertySerializationHelper.ReadProperties(reader, ProspectSerlializationUtil.IcarusPackageVersion, false));
				}
			}

			return instance;
		}

		/// <summary>
		/// Saves this prospect to the given stream
		/// </summary>
		/// <param name="stream">The stream to write to</param>
		public void Save(Stream stream)
		{
			using (MemoryStream mem = new())
			{
				using (BinaryWriter writer = new(mem, Encoding.ASCII, true))
				{
					PropertySerializationHelper.WriteProperties(mProspectData, writer, ProspectSerlializationUtil.IcarusPackageVersion, false);
				}
				mProspectBlob.UncompressedLength = (int)mem.Length;

				mem.Seek(0, SeekOrigin.Begin);
				using (SHA1 sha1 = SHA1.Create())
				{
					mProspectBlob.Hash = Convert.ToHexString(sha1.ComputeHash(mem)).ToLowerInvariant();
				}

				mem.Seek(0, SeekOrigin.Begin);
				using (MemoryStream memCompressed = new())
				{
					using (ZlibStream zstream = new(memCompressed, CompressionMode.Compress, true))
					{
						zstream.Write(mem.ToArray(), 0, (int)mem.Length);
						zstream.Flush(); // For some reason, this stream does not seem to flush on Dispose
					}
					mProspectBlob.DataLength = (int)memCompressed.Length;
					mProspectBlob.TotalLength = (int)memCompressed.Length;
					mProspectBlob.BinaryBlob = Convert.ToBase64String(memCompressed.ToArray());
				}
			}

			JsonSerializer serializer = new();
			serializer.Formatting = Formatting.Indented;
			serializer.NullValueHandling = NullValueHandling.Ignore;

			using (StreamWriter writer = new(stream))
			{
				serializer.Serialize(writer, this);
			}
		}
	}

	public struct FProspectInfo
	{
		public string ProspectID;
		public string ClaimedAccountID;
		public int ClaimedAccountCharacter;
		public string ProspectDTKey;
		public string FactionMissionDTKey;
		public string LobbyName;
		public long ExpireTime;
		public string ProspectState;
		public List<FAssociatedMember> AssociatedMembers;
		public int Cost;
		public int Reward;
		public string Difficulty;
		public bool Insurance;
		public bool NoRespawns;
		public int ElapsedTime;
		public int SelectedDropPoint;
	}

	public struct FProspectBlob
	{
		public string Key;
		public string Hash;
		public int TotalLength;
		public int DataLength;
		public int UncompressedLength;
		public string BinaryBlob;
	}

	public struct FAssociatedMember
	{
		public string AccountName;
		public string CharacterName;
		public string UserID;
		public int ChrSlot;
		public int Experience;
		public string Status;
		public bool Settled;
		public bool IsCurrentlyPlaying;
	}
}
