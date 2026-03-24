using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Fuxion;
using Fuxion.ComponentModel.DataAnnotations;
using Fuxion.Text.Json;
using Fuxion.Xunit;
using Xunit;

namespace Test.Fuxion.ComponentModel.DataAnnotations;

public class ValidationTest(ITestOutputHelper output) : BaseTest<ValidationTest>(output)
{
	[Fact]
	public void ValidateModel()
	{
		var mm = new Model()
		{
			Name = ""
		};
		var tt = mm.Fx.Validation.ToResponse();

		Model? model = null;

		var res = model.Fx.Validation.ToResponse();
		IsTrue(res.IsError);
		PrintVariable(res.Fx.Json.Serialize(true).PayloadOrDefault());

		res = model.Fx.Validation.ToResponse(true);
		IsTrue(res.IsSuccess);
		PrintVariable(res.Fx.Json.Serialize(true).PayloadOrDefault());

		model = new()
		{
			Name = "Bob",
			Comments = "12345678"
		};
		res = model.Fx.Validation.ToResponse();
		IsTrue(res.IsError);
		PrintVariable(res.Fx.Json.Serialize(true).PayloadOrDefault());

		model.Name = "Alice";
		model.Age = 28;
		model.Comments = "123456";

		res = model.Fx.Validation.ToResponse();
		IsTrue(res.IsSuccess);
		PrintVariable(res.Fx.Json.Serialize(true).PayloadOrDefault());
	}

	[Fact]
	public void ValidateValidatable()
	{
		Validatabe? validatable = null;

		var res = validatable.Fx.Validation.ToResponse();
		IsTrue(res.IsError);
		PrintVariable(res.Fx.Json.Serialize(true).PayloadOrDefault());

		res = validatable.Fx.Validation.ToResponse(true);
		IsTrue(res.IsSuccess);
		PrintVariable(res.Fx.Json.Serialize(true).PayloadOrDefault());

		validatable = new()
		{
			Name = "Bob",
			Comments = "12345678"
		};

		res = validatable.Fx.Validation.ToResponse();
		IsTrue(res.IsError);
		PrintVariable(res.Fx.Json.Serialize(true).PayloadOrDefault());

		validatable.Name = "Alice";
		validatable.Age = 28;
		validatable.Comments = "123456";

		res = validatable.Fx.Validation.ToResponse();
		IsTrue(res.IsSuccess);
		PrintVariable(res.Fx.Json.Serialize(true).PayloadOrDefault());
	}
}

public class Model
{
	public required string Name { get; set; }
	[NonZero] public int Age { get; set; }
	[MaxLength(6)] public string? Comments { get; set; }
}

public class Validatabe : IValidatableObject
{
	public required string Name { get; set; }
	[NonZero] public int Age { get; set; }
	[MaxLength(6)] public string? Comments { get; set; }

	public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
	{
		if (Name == "Bob")
			yield return new("Name cannot be Bob");
	}
}

public class NonZeroAttribute : ValidationAttribute
{
	protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
	{
		if (value is > 0)
			return ValidationResult.Success;
		return new("Value is zero or not int.");
	}

	public override bool IsValid(object? value)
	{
		if (value is > 0)
			return true;
		return base.IsValid(value);
	}
}