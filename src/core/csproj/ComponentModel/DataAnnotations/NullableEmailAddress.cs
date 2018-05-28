﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace System.ComponentModel.DataAnnotations
{
    public class NullableEmailAddress : DataTypeAttribute
    {
		public NullableEmailAddress() : base(DataType.EmailAddress) { }
		public override bool IsValid(object value)
			=> value == null || value is string input && (string.IsNullOrEmpty(input) || new EmailAddressAttribute().IsValid(input));
	}
}
