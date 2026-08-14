var stream = new EventStream<Empresa>();
var registrarEmpresaHandler = new RegistrarEmpresaHandler(stream);
var cambiarPlanHandler = new CambiarPlanHandler(stream);
var suspenderHandler = new SuspenderHandler(stream);
var reactivarHandler = new ReactivarHandler(stream);

registrarEmpresaHandler.Handle("Constructora Andes", "Basico");
suspenderHandler.Handle("Falta de pago");
reactivarHandler.Handle();
cambiarPlanHandler.Handle("Premium");
suspenderHandler.Handle("Incumplimiento de contrato");
reactivarHandler.Handle();

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

public class RegistrarEmpresaHandler(EventStream<Empresa> stream)
{
    public void Handle(string nombre, string plan)
    {
        var andes = stream.Get();
        stream.Append(andes.Registrar(nombre, plan));
    }
}

public class CambiarPlanHandler(EventStream<Empresa> stream)
{
    public void Handle(string plan)
    {
        var andes = stream.Get();
        stream.Append(andes.CambiarPlan(plan));
    }
}

public class SuspenderHandler(EventStream<Empresa> stream)
{
    public void Handle(string motivo)
    {
        var andes = stream.Get();
        var empresaSuspendida2 = andes.Suspender(motivo);
        if(empresaSuspendida2 is not null)
        {
            stream.Append(empresaSuspendida2); 
        }       
    }
}

public class ReactivarHandler(EventStream<Empresa> stream)
{
    public void Handle()
    {
        var andes = stream.Get();
        stream.Append(andes.Reactivar());
    }
}

//eventos
public record EmpresaRegistrada(string Nombre, string Plan);
public record PlanCambiado(string PlanNuevo);
public record EmpresaSuspendida(string Motivo);
public record EmpresaReactivada();
