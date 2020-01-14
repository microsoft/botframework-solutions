
// NOTE: Generated code may require at least .NET Framework 4.5 or .NET Core/Standard 2.0.
/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
[System.Xml.Serialization.XmlRootAttribute(Namespace = "", IsNullable = false)]
public partial class getRestaurants
{

    private getRestaurantsRestaurant[] restaurantsField;

    private string currenttimeField;

    private string statusField;

    private string generatorField;

    private decimal versionField;

    /// <remarks/>
    [System.Xml.Serialization.XmlArrayItemAttribute("restaurant", IsNullable = false)]
    public getRestaurantsRestaurant[] restaurants
    {
        get
        {
            return this.restaurantsField;
        }
        set
        {
            this.restaurantsField = value;
        }
    }

    /// <remarks/>
    public string currenttime
    {
        get
        {
            return this.currenttimeField;
        }
        set
        {
            this.currenttimeField = value;
        }
    }

    /// <remarks/>
    public string status
    {
        get
        {
            return this.statusField;
        }
        set
        {
            this.statusField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public string generator
    {
        get
        {
            return this.generatorField;
        }
        set
        {
            this.generatorField = value;
        }
    }

    /// <remarks/>
    [System.Xml.Serialization.XmlAttributeAttribute()]
    public decimal version
    {
        get
        {
            return this.versionField;
        }
        set
        {
            this.versionField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class getRestaurantsRestaurant
{

    private string idField;

    private string nameField;

    private string branchnameField;

    private string logourlField;

    private string restauranturlField;

    private getRestaurantsRestaurantGeolocation geolocationField;

    private string openField;

    private string reviewratingField;

    private string categoriesField;

    /// <remarks/>
    public string id
    {
        get
        {
            return this.idField;
        }
        set
        {
            this.idField = value;
        }
    }

    /// <remarks/>
    public string name
    {
        get
        {
            return this.nameField;
        }
        set
        {
            this.nameField = value;
        }
    }

    /// <remarks/>
    public string branchname
    {
        get
        {
            return this.branchnameField;
        }
        set
        {
            this.branchnameField = value;
        }
    }

    /// <remarks/>
    public string logourl
    {
        get
        {
            return this.logourlField;
        }
        set
        {
            this.logourlField = value;
        }
    }

    /// <remarks/>
    public string restauranturl
    {
        get
        {
            return this.restauranturlField;
        }
        set
        {
            this.restauranturlField = value;
        }
    }

    /// <remarks/>
    public getRestaurantsRestaurantGeolocation geolocation
    {
        get
        {
            return this.geolocationField;
        }
        set
        {
            this.geolocationField = value;
        }
    }

    /// <remarks/>
    public string open
    {
        get
        {
            return this.openField;
        }
        set
        {
            this.openField = value;
        }
    }

    /// <remarks/>
    public string reviewrating
    {
        get
        {
            return this.reviewratingField;
        }
        set
        {
            this.reviewratingField = value;
        }
    }

    /// <remarks/>
    public string categories
    {
        get
        {
            return this.categoriesField;
        }
        set
        {
            this.categoriesField = value;
        }
    }
}

/// <remarks/>
[System.SerializableAttribute()]
[System.ComponentModel.DesignerCategoryAttribute("code")]
[System.Xml.Serialization.XmlTypeAttribute(AnonymousType = true)]
public partial class getRestaurantsRestaurantGeolocation
{

    private string latitudeField;

    private string longitudeField;

    /// <remarks/>
    public string latitude
    {
        get
        {
            return this.latitudeField;
        }
        set
        {
            this.latitudeField = value;
        }
    }

    /// <remarks/>
    public string longitude
    {
        get
        {
            return this.longitudeField;
        }
        set
        {
            this.longitudeField = value;
        }
    }
}

