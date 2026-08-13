var stream = new EventStream<Empresa>();
var andes = stream.Get();
stream.Append(andes.Registrar("Constructora Andes", "Basico"));

andes = stream.Get();
var empresaSuspendida = andes.Suspender("Falta de pago");
if(empresaSuspendida is not null)
{
    stream.Append(empresaSuspendida); 
}

andes = stream.Get();
var act = andes.Reactivar();
stream.Append(act);

andes = stream.Get();
stream.Append(andes.CambiarPlan("Premium"));

andes = stream.Get();
var empresaSuspendida2 = andes.Suspender("Incumplimiento de contrato");
if(empresaSuspendida2 is not null)
{
    stream.Append(empresaSuspendida2); 
}

andes = stream.Get();
stream.Append(andes.Reactivar());

var andes2Version = stream.Get();

Console.WriteLine($"{andes2Version.nombre}: plan {andes2Version.plan}, {(andes2Version.suspendida ? "suspendida" : "activa")}, reactivada {andes2Version.reactivaciones} vez/veces");

//Clases
public class Empresa : AgregateRoot
{
    public string nombre {get; private set;} = "";
    public string plan {get; private set;}= "";
    public bool suspendida {get; private set;} = false;
    public int reactivaciones {get; private set;} = 0;

    protected override void Aplicar(Object evento)
    {
        switch (evento)
        {
            case EmpresaRegistrada r:
                nombre = r.Nombre;
                plan = r.Plan;
                break;
            case PlanCambiado c:
                plan = c.PlanNuevo;
                break;
            case EmpresaSuspendida s:
                suspendida = true;
                break;
            case EmpresaReactivada er:
                suspendida = false;
                reactivaciones++;
                break;
        }
    }

    public EmpresaRegistrada Registrar(string Nombre, string Plan) => new EmpresaRegistrada(Nombre, Plan);

    public PlanCambiado CambiarPlan(string PlanNuevo)
    {
        if(suspendida)
            throw new Exception("Empresa esta suspendida");

        return new(PlanNuevo);   
    }

    public EmpresaSuspendida? Suspender(string Motivo)
    {
        return suspendida? null: new(Motivo);
    }

    public EmpresaReactivada Reactivar() => new();
}

public abstract class AgregateRoot
{
    public void Load(IEnumerable<Object> stream)
    {
        foreach (var evento in stream)
        {
            Aplicar(evento);
        }
    }

    protected abstract void Aplicar(Object evento);
}

public class EventStream<T> where T : AgregateRoot, new()
{
    private readonly List<Object> _stream = new();

    public T Get()
    {
        var miEmpresa = new T();
        miEmpresa.Load(_stream);

        return miEmpresa;
    }

    public void Append(Object evento)
    {
        _stream.Add(evento);
    }
}

//eventos
public record EmpresaRegistrada(string Nombre, string Plan);
public record PlanCambiado(string PlanNuevo);
public record EmpresaSuspendida(string Motivo);
public record EmpresaReactivada();
