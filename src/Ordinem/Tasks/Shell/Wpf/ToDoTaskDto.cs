﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Ordinem.Tasks.Shell.Wpf
{
	public class ToDoTaskDto
	{
		public ToDoTaskDto(Guid id, string name)
		{
			Id = id;
			Name = name;
		}
		public Guid Id { get; set; } = Guid.NewGuid();
		public string Name { get; set; }
	}
}
