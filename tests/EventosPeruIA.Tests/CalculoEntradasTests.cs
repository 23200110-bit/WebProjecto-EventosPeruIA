using EventosPeruIA.CORE.Core.DTOs;
using EventosPeruIA.CORE.Core.Services;
using Xunit;

public class CalculoEntradasTests
{
    private static EventoChatDTO Evento(decimal precio = 85, int cupos = 499) => new(1, "Rock en Lima 2026", "Concierto", "Conciertos", DateTime.UtcNow.AddMonths(1), null, "Lima", precio, cupos);
    [Theory]
    [InlineData("Somos 3 personas. ¿Cuánto debemos tener en total para las entradas?", "S/ 255.00")]
    [InlineData("Somos tres personas. ¿Cuánto cuesta en total?", "S/ 255.00")]
    [InlineData("Tengo 100 soles y quiero ir con una persona más. ¿Me alcanza y cuánto me sobra?", "S/ 70.00")]
    [InlineData("Somos 2 personas, presupuesto total 170 soles. ¿Alcanza?", "Alcanza exactamente")]
    [InlineData("Somos 2 personas y tenemos 100 soles por persona. ¿Alcanza?", "S/ 30.00")]
    [InlineData("Somos 2 personas y tenemos 50 soles cada uno. ¿Alcanza?", "S/ 70.00")]
    [InlineData("Somos 0 personas. ¿Cuánto cuesta?", "cantidad entera")]
    [InlineData("Somos 2.5 personas. ¿Cuánto cuesta?", "cantidad entera")]
    [InlineData("Somos -2 personas. ¿Cuánto cuesta?", "cantidad entera")]
    [InlineData("Somos 1001 personas. ¿Cuánto cuesta?", "cantidad entera")]
    [InlineData("Tengo -50 soles. ¿Alcanza?", "no negativo")]
    [InlineData("Somos 2 personas con 100 dólares en total. ¿Alcanza?", "tipo de cambio")]
    [InlineData("Tengo 1,000 soles. ¿Alcanza?", "sin separadores")]
    [InlineData("Voy con amigos, ¿cuánto cuesta?", "cantidad entera")]
    [InlineData("Somos 3 personas. ¿Hay precio con descuento?", "tarifas especiales")]
    public void Calcula_o_aclara_sin_dejar_operaciones_al_modelo(string mensaje, string esperado)
    {
        var result = CalculoEntradas.Responder(Evento(), [new("user", mensaje)]);
        Assert.NotNull(result);
        Assert.Contains(esperado, result.Texto);
    }
    [Fact]
    public void Actualiza_cantidad_y_presupuesto_sin_heredar_alcance_anterior()
    {
        List<TurnoChat> history = [new("user", "Somos tres y tenemos 100 soles por persona. ¿Alcanza?"), new("user", "Ahora somos 2 personas y el presupuesto total es 150 soles. ¿Alcanza?")];
        var result = CalculoEntradas.Responder(Evento(), history)!;
        Assert.Contains("S/ 170.00", result.Texto);
        Assert.Contains("S/ 20.00", result.Texto);
        Assert.False(result.Recomendar);
    }
    [Fact]
    public void Conserva_cantidad_para_seguimiento_y_solicita_aclaracion_de_presupuesto()
    {
        var result = CalculoEntradas.Responder(Evento(), [new("user", "Somos 3 personas"), new("assistant", "Hola"), new("user", "Tengo 100 soles. ¿Alcanza?")])!;
        Assert.Contains("S/ 255.00", result.Texto);
        Assert.Contains("total o por persona", result.Texto);
        Assert.False(result.Recomendar);
    }
    [Fact]
    public void Disponibilidad_y_gratuidad()
    {
        Assert.False(CalculoEntradas.Responder(Evento(cupos: 1), [new("user", "Somos 2 personas. ¿Cuánto cuesta?")])!.Recomendar);
        Assert.Contains("S/ 0.00", CalculoEntradas.Responder(Evento(precio: 0), [new("user", "Somos 3 personas. ¿Cuánto cuesta en total?")])!.Texto);
    }
    [Fact]
    public void Pregunta_de_fecha_no_se_convierte_en_presupuesto()
    {
        Assert.Null(CalculoEntradas.Responder(Evento(), [new("user", "Tengo 50 soles"), new("user", "¿En qué fecha empieza?")]));
    }
    [Fact]
    public void Aclaracion_por_persona_recalcula_sin_repetir_monto()
    {
        var result = CalculoEntradas.Responder(Evento(), [new("user", "Somos dos y tengo 100 soles. ¿Alcanza?"), new("user", "Es por persona")])!;
        Assert.Contains("S/ 30.00", result.Texto);
        Assert.True(result.Recomendar);
    }
}
