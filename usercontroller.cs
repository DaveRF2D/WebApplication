using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using AsiaAdmin.Models;


namespace AsiaAdmin.App_Start
{
    //https://www.codemag.com/article/1601031/CRUD-in-HTML-JavaScript-and-jQuery-Using-the-Web-API 

    public class usuarioApi
    {
        //public int ProductId { get; set; }
        public string Password { get; set; }
        public string UserName { get; set; }
        public DateTime fechacreacion { get; set; }
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        //Solo puede tener un tipo de rol inicialmente  
        public string role { get; set; }
    }

    public class UserController : ApiController
    {
        ///declaramos la cadena de conexión para poder cambiarla en cualquier momento y que sea global
        public string conexion = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        ///sacamos el rol de un usuario en concreto
        private string getRole(string email)
        {
            string valorRol = "";
            using (SqlConnection connection = new SqlConnection(conexion))
            {
                string queryString = @"SELECT TOP (1) AspNetUsuariosRoles.UserId, AspNetUsuariosRoles.RoleId, AspNetUsuariosRoles.IdentityUser_Id, 
                    AspNetRoles.Name FROM AspNetUsuariosRoles INNER JOIN AspNetRoles ON AspNetUsuariosRoles.RoleId = AspNetRoles.Id 
                    where userid=(SELECT Id FROM AspNetUsuarios WHERE(email = @email))";

                connection.Open();               
                SqlCommand cmd = new SqlCommand(queryString, connection);
                cmd.Parameters.AddWithValue("email", email);
                SqlDataReader dr = cmd.ExecuteReader();
                if(dr.Read())
                {
                    valorRol = dr["Name"].ToString();
                }
            }
            return valorRol;
        }

        ///Actualizamos el rol de un usuario en concreto
        private string setRole(string email,string role)
        {
            string valorRol = "";
            using (SqlConnection connection = new SqlConnection(conexion))
            {
                string queryString;

                if (getRole(email) == "")
                {
                    //queryString = "INSERT INTO AspNetUsuariosRoles SELECT (SELECT Id FROM AspNetUsuarios WHERE(UserName = @username)) as Userid, id FROM AspNetRoles WHERE(Name = @Name)";
                    queryString= @"INSERT INTO AspNetUsuariosRoles(UserId, RoleId) SELECT asid, Usid 
                        FROM(SELECT  Id AS asid, (SELECT  Id FROM AspNetRoles WHERE(Name = @name)) AS Usid 
                        FROM AspNetUsuarios AS as2 WHERE(email = @email)) AS derivedtbl_1";                    
                }
                else
                {
                    queryString = @"UPDATE TOP (1) AspNetUsuariosRoles SET RoleId = (SELECT id FROM AspNetRoles where Name = @name) , 
                    userid =(SELECT Id FROM AspNetUsuarios WHERE(email = @email)) 
                    where userid=(SELECT Id FROM AspNetUsuarios WHERE(email = @email))";

                    role = getRole(email);
                }
                connection.Open();
                SqlCommand cmd = new SqlCommand(queryString, connection);
                cmd.Parameters.AddWithValue("email", email);
                cmd.Parameters.AddWithValue("name", role);
                cmd.ExecuteNonQuery();
            }
            return valorRol;
        }

        /// <summary>
        /// sacamos todos los usuarios 
        /// </summary>
        // GET: api/Db
        [HttpGet()]
        // public IEnumerable<ApplicationUser> Get()
        public IHttpActionResult Get()
        {
            IHttpActionResult ret = null;
            //List<ApplicationUser> logins = new List<ApplicationUser>();
            List<usuarioApi> logins = new List<usuarioApi>();
            if (User.Identity.IsAuthenticated)
            {
                foreach (ApplicationUser user in new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())).Users)
                {
                      logins.Add(new usuarioApi
                      {
                          Id = user.Id,
                          UserName = user.UserName,
                          Email = user.Email,
                          FirstName=user.FirstName,
                          LastName=user.LastName,
                          fechacreacion=user.Fechacreacion,
                          role=getRole(user.Email)
                      });
                }
            }
            if(logins.Count>0)
            {
                ret = Ok(logins);
            }
            else
            {
                ret = NotFound();
            }
           // ret = (logins.Count > 0) ? Ok(logins) : NotFound();
            return ret;
        }

        /// GET: api/Db/5
        [HttpGet()]
        //public ApplicationUser Get(string username)
        public IHttpActionResult Get(string Id)         
        {
            IHttpActionResult ret;
           // List<ApplicationUser> logins = new List<ApplicationUser>();
            List<usuarioApi> logins = new List<usuarioApi>();
            
            foreach (ApplicationUser user in new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())).Users)
            {
                if (user.Id== Id)
                {
                    logins.Add(new usuarioApi
                    {
                        Id = user.Id,
                        UserName = user.UserName,
                        Email = user.Email,
                        FirstName=user.FirstName,
                        LastName=user.LastName,
                        fechacreacion = user.Fechacreacion,
                        role=getRole(user.Email)
                    });                  
                }
            }
            if (logins.Count > 0)
            {
                ret = Ok(logins);
            }
            else
            {
                ret = NotFound();
            }
           // ret = (logins.Count > 0)? Ok(logins): NotFound();
            return ret; // return new string[] { "value1", "value2" };
        }

        /// POST: api/Db //nuevo usuario
        [HttpPost]
        public IHttpActionResult Post(usuarioApi usuarioNuevo)
        {
            IHttpActionResult ret = null;
            var userStore = new UserStore<ApplicationUser>(new ApplicationDbContext());
            var manager = new UserManager<ApplicationUser>(userStore);
            manager.UserValidator = new UserValidator<AsiaAdmin.Models.ApplicationUser>(manager)
            {
                AllowOnlyAlphanumericUserNames = false,
                RequireUniqueEmail = true
            };
            //var user = new IdentityUser() { UserName = UserName.Text };
            var user = new ApplicationUser() 
            {
                UserName = usuarioNuevo.UserName, FirstName=usuarioNuevo.FirstName, LastName=usuarioNuevo.LastName, 
                Email = usuarioNuevo.Email ,Fechacreacion=DateTime.Now
            };

            //var user = new ApplicationUser() { UserName = usuarioNuevo.UserName };           
            //manager.AddToRoles(usuarioNuevo.Id, new string[] { usuarioNuevo.role });
            IdentityResult result = manager.Create(user, usuarioNuevo.Password);
            setRole(usuarioNuevo.Email, usuarioNuevo.role);
            if (result.Succeeded)
            {
                ret = Created<ApplicationUser>(Request.RequestUri + usuarioNuevo.Email, user);
            }
            else
            {
                ret = NotFound();// NotFound();
            }
           // ret = (result.Succeeded)? Created<ApplicationUser>(Request.RequestUri + usuarioNuevo.UserName, user) : NotFound();
            return ret;
        }

        ///Actualizar usuario
        /// PUT: api/Db/5 //Actualizar usuario
        [HttpPut]
        public IHttpActionResult Put(usuarioApi usuario)
        {
            IHttpActionResult ret = null;
            if (Update(usuario))
            {
                //recogemos los cambios del usuario
                List<usuarioApi> logins = new List<usuarioApi>();

                foreach (ApplicationUser user in new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext())).Users)
                {
                    if (user.Id == usuario.Id)
                    {
                        usuario.Id = user.Id;
                        usuario.UserName = user.UserName;
                        usuario.Email = user.Email;
                        usuario.FirstName = user.FirstName;
                        usuario.LastName = user.LastName;
                        usuario.fechacreacion = user.Fechacreacion;
                        usuario.role = getRole(user.UserName);                       
                    }
                }
                ret = Ok(usuario);
            }
            else
            {
                ret = NotFound();
            }
            return ret;
        }

        private bool GetUsuario(usuarioApi usuario)
        {
            var userStore = new UserStore<ApplicationUser>(new ApplicationDbContext());
            var manager = new UserManager<ApplicationUser>(userStore);
            var newPasswordHash = manager.PasswordHasher.HashPassword(usuario.Password);
            //Actualizamos por base de datos los valores de password podríamos hacerlo directamente a traves de identity pero vamos a probar así si funciona
            using (SqlConnection connection = new SqlConnection(conexion))
            {
                string queryString = (usuario.Password == "")? "UPDATE aspnetusuarios SET username=@username,email=@email,FirstName=@firstname,LastName=@lastname WHERE id=@id" : "UPDATE aspnetusuarios SET passwordhash=@password,username=@username,email=@email,FirstName=@firstname,LastName=@lastname WHERE id=@id";
                connection.Open();
                SqlCommand cmd = new SqlCommand(queryString, connection);
                cmd.Parameters.AddWithValue("id", usuario.Id);
                cmd.Parameters.AddWithValue("username", usuario.UserName);
                cmd.Parameters.AddWithValue("email", usuario.Email);
                cmd.Parameters.AddWithValue("password", newPasswordHash);
                cmd.Parameters.AddWithValue("firstname", usuario.FirstName);
                cmd.Parameters.AddWithValue("lastname", usuario.LastName);
                cmd.ExecuteNonQuery();
                connection.Close();

                setRole(usuario.Email, usuario.role);
                //var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
                //  manager.AddToRoles(usuario.Id, new string[] { usuario.role });

            }
            return true;
        }
        private bool Update(usuarioApi usuario)
        {
            var userStore = new UserStore<ApplicationUser>(new ApplicationDbContext());
            var manager = new UserManager<ApplicationUser>(userStore);
            var newPasswordHash =manager.PasswordHasher.HashPassword(usuario.Password);
            //Actualizamos por base de datos los valores de password podríamos hacerlo directamente a traves de identity pero vamos a probar así si funciona
            using (SqlConnection connection = new SqlConnection(conexion))
            {
                string queryString = (usuario.Password == "")? "UPDATE aspnetusuarios SET username=@username,email=@email,FirstName=@firstname,LastName=@lastname WHERE id=@id": "UPDATE aspnetusuarios SET passwordhash=@password,username=@username,email=@email,FirstName=@firstname,LastName=@lastname WHERE id=@id";
                connection.Open();
                SqlCommand cmd = new SqlCommand(queryString, connection);     
                cmd.Parameters.AddWithValue("id", usuario.Id);
                cmd.Parameters.AddWithValue("username", usuario.UserName);
                cmd.Parameters.AddWithValue("email", usuario.Email);
                cmd.Parameters.AddWithValue("password", newPasswordHash);
                cmd.Parameters.AddWithValue("firstname", usuario.FirstName);
                cmd.Parameters.AddWithValue("lastname", usuario.LastName);
                cmd.ExecuteNonQuery();
                connection.Close();
                
                setRole(usuario.Email, usuario.role);
                //var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
              //  manager.AddToRoles(usuario.Id, new string[] { usuario.role });
                
            }return true;
        }

        /// DELETE: api/Db/5
        public void Delete(int id)
        {
        }
    }
}
