namespace WebApi.Models;

public class UserDto
{
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public string Username { get; set; }
    
    public string Email { get; set; }
    
    public AddressUnit Address { get; set; }
    
    public string Phone { get; set; }
    
    public string Website { get; set; }
    
    public CompanyUnit Company { get; set; }
    
    public class AddressUnit
    {
        public string Street { get; set; }
        
        public string Suite { get; set; }
        
        public string City { get; set; }
        
        public string Zipcode { get; set; }
        
        public GeoUnit Geo { get; set; }
    }

    public class CompanyUnit
    {
        public string Name { get; set; }
        
        public string CatchPhrase { get; set; }
        
        public string Bs { get; set; }
    }

    public class GeoUnit
    {
        public string Lat { get; set; }
        
        public string Lng { get; set; }
    }
}