// RedVital — campaign-service. Tipos comunes de T-312.2, T-312.3 y T-312.4.

using RedVital.Campanas.Dominio;

namespace RedVital.Campanas.Aplicacion.CasosDeUso;

public enum Fallo { Ninguno, NoPermitido, NoEncontrado, Conflicto, ReglaNegocio }

public record Resultado<T>(T? Valor, Fallo Fallo = Fallo.Ninguno)
{
    public bool Exitoso => Fallo == Fallo.Ninguno;
    public static Resultado<T> Ok(T valor) => new(valor);
    public static Resultado<T> Error(Fallo fallo) => new(default, fallo);
}

/// <summary>
/// Forma pública de una campaña (CampaniaResponse del contrato). Expone
/// cupo_disponible y NUNCA cupo_reservado.
/// </summary>
public record CampaniaRespuesta(
    Guid Id, Guid InstitucionId, string TerritorioCodigo, string TerritorioRuta,
    string Nombre, string? Descripcion, string Sede,
    DateTimeOffset IniciaEn, DateTimeOffset TerminaEn, string Estado,
    int? CupoTotal, int? CupoDisponible, DateTimeOffset? PublicadaEn)
{
    public static CampaniaRespuesta Desde(Campania c) => new(
        c.Id, c.InstitucionId, c.TerritorioCodigo, c.TerritorioRuta, c.Nombre, c.Descripcion, c.Sede,
        c.IniciaEn, c.TerminaEn, c.Estado, c.CupoTotal, c.CupoDisponible, c.PublicadaEn);
}

public static class Auditar
{
    public static RegistroAuditoriaCamp Hecho(Solicitante s, string operacion, Guid? recursoId, string resultado,
        string correlacionId, string? jurisdiccionSolicitada = null) => new()
    {
        Id = Guid.NewGuid(),
        ActorTipo = s.EsAnonimo ? "anonimo" : s.Rol == "servicio" ? "sistema" : "usuario",
        ActorId = s.Sub,
        Rol = s.Rol,
        JurisdiccionSolicitada = jurisdiccionSolicitada,
        Operacion = operacion,
        RecursoTipo = "campania",
        RecursoId = recursoId,
        Resultado = resultado,
        CorrelacionId = correlacionId,
        OcurridoEn = DateTimeOffset.UtcNow,
    };
}
