/*
 * Copyright (c) 2014 - 2022 t_saki@serenegiant.com 
 */
using System;
using UnityEngine;

[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
public class ComponentRestrictionAttribute : PropertyAttribute
{
	public readonly Type type;
	public ComponentRestrictionAttribute(Type type)
	{
		this.type = type;
	}

} // class ComponentRestrictionAttribute

