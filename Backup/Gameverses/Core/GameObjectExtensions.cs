using System;
using System.Reflection;
using UnityEngine;


public static class GameObjectExtensions
{
	
	public static void SetLayerRecursively(this GameObject inst, int layer)
	{
		inst.layer = layer;
		foreach(Transform child in inst.transform)
			child.gameObject.SetLayerRecursively(layer);
	}
	
	/// <summary>
	/// Adds all the components found on a resource prefab.
	/// </summary>
	/// <param name='inst'>
	/// Instance of game object to add the components to
	/// </param>
	/// <param name='path'>
	/// Path of prefab relative to ANY resource folder in the assets directory
	/// </param>
	/// 
	public static void AddComponentsFromResource(this GameObject inst, string path)
	{
		var go = Resources.Load(path) as GameObject;
		
		foreach(var src in go.GetComponents<Component>()) {
			var dst = inst.AddComponent(src.GetType()) as Behaviour;
			dst.enabled = false;
			Gameverses.ComponentUtil.Copy(dst, src);
			dst.enabled = true;
		}
	}
	
	/// <summary>
	/// Adds a component of the specific type found on a resource prefab.
	/// </summary>
	/// <returns>
	/// The newly added component.
	/// </returns>
	/// <param name='inst'>
	/// Instance of game object to add the component to
	/// </param>
	/// <param name='path'>
	/// Path of prefab relative to ANY resource folder in the assets directory
	/// </param>
	/// <typeparam name='T'>
	/// The type of component to find on the prefab and add.
	/// </typeparam>
	/// <exception cref='ArgumentException'>
	/// Is thrown when the path is invalid.
	/// </exception>
	/// 
	public static T AddComponentFromResource<T>(this GameObject inst, string path)
		where T : Component
	{
		var go = Resources.Load(path) as GameObject;
		if(go == null)
			throw new ArgumentException("Invalid component path", "path");
		
		var src = go.GetComponent<T>();
		var dst = inst.AddComponent<T>();
		
		Gameverses.ComponentUtil.Copy(dst, src);
		
		return dst;
	}
	
	/// <summary>
	/// Gets a component from a game object (supports interfaces)
	/// </summary>
	/// <returns>
	/// The component found in the game object
	/// </returns>
	/// <param name='inst'>
	/// Instance of game object to add the component to
	/// </param>
	/// <typeparam name='T'>
	/// The type of component, or interface, to find
	/// </typeparam>
	/// 
	public static T GetComponent<T>(this GameObject inst)
		where T : class
	{
		return inst.GetComponent(typeof(T)) as T;
	}
	
	public static Transform FindBelow(this GameObject inst, string name)
	{
		if (inst.transform.childCount == 0) {
			return null;
		}
		var child = inst.transform.Find(name);
		if (child != null) {
			return child;
		}
		foreach (GameObject t in inst.transform) {
			child = FindBelow(t, name);
			if (child != null) {
				return child;
			}
		}
			return null;
		}
		
	}