using Microsoft.Maui.Controls;
using System;

namespace ControlGastosApp;

public partial class NuevoMovimientoPage : ContentPage
{
	public NuevoMovimientoPage()
	{
		InitializeComponent();
	}

	//Se ejecuta al presionar el boton Cancelar
	private async void OnCancelarClicked(object sender, EventArgs e)
	{
		//Regresa de inmediato a la pantalla principal sin hacer nada
		await Navigation.PopModalAsync();
	}

	//Se ejecuta al precionar el boton de Guardar
	private async void OnGuardarClicked(object sender, EventArgs e)
	{
		//Aqui meteremos la logica de SQLite
		await Navigation.PopModalAsync();
	}
}