function InicializarFormularioTransacciones(urlObtenerCateorias) {
    $("#TipoOperacionId").change(async function () {
        const valorSeleccionado = $(this).val();

        const respuesta = await fetch(urlObtenerCategorias, {
            method: 'POST',
            body: valorSeleccionado,
            headers: {
                'Content-Type': 'application/json'
            }
        });

        const json = await respuesta.json();
        // bagting ``
        // arreglo de opciones 
        const opciones = json.map(categoria => `<option value=${categoria.value}>${categoria.text}</option>`);

        $("#CategoriaId").html(opciones);
    })
}