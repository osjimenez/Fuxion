﻿using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Fuxion.Identity.Helpers
{
	public class PasswordProvider : IPasswordProvider
	{
		private static RNGCryptoServiceProvider saltGenerator = new RNGCryptoServiceProvider();

		public int SaltBytesLenght { get; set; } = 8;
		public int HashIterations { get; set; } = 10137;
		public PasswordHashAlgorithm Algorithm { get; set; } = PasswordHashAlgorithm.SHA256;

		private HashAlgorithm GetAlgorithm()
		{
			switch (Algorithm)
			{
				case PasswordHashAlgorithm.SHA1:
					return new SHA1Managed();
				case PasswordHashAlgorithm.SHA256:
					return new SHA256Managed();
				case PasswordHashAlgorithm.SHA384:
					return new SHA384Managed();
				case PasswordHashAlgorithm.SHA512:
					return new SHA512Managed();
				default:
					throw new ArgumentException($"Value '{Algorithm}' for '{nameof(Algorithm)}' is not valid");
			}
		}
		internal void Generate(string password, byte[] salt, out byte[] hash)
		{
			var saltAndPass = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();
			var hashProvider = GetAlgorithm();
			hash = hashProvider.ComputeHash(saltAndPass);
			for (var i = 0; i < HashIterations; i++)
				hash = hashProvider.ComputeHash(hash);
		}
		public void Generate(string password, out byte[] salt, out byte[] hash)
		{
			salt = new byte[SaltBytesLenght];
			saltGenerator.GetNonZeroBytes(salt);
			Generate(password, salt, out hash);
		}
		public bool Verify(string password, byte[] hash, byte[] salt)
		{
			var saltAndPass = Encoding.UTF8.GetBytes(password).Concat(salt).ToArray();
			var hashProvider = GetAlgorithm();
			var computeHash = hashProvider.ComputeHash(saltAndPass);
			for (var i = 0; i < HashIterations; i++)
				computeHash = hashProvider.ComputeHash(computeHash);
			return computeHash.SequenceEqual(hash);
		}
	}
	public enum PasswordHashAlgorithm
	{
		SHA1 = 20,
		SHA256 = 32,
		SHA384 = 48,
		SHA512 = 64
	}
}