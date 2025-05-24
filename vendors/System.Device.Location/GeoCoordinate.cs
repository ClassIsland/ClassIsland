// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*=============================================================================
**
** Class: GeoLocationCoordinate
**
** Purpose: Represents a GeoCoordinate object
**
=============================================================================*/

using System;
using System.Globalization;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Strings.Resources;

namespace System.Device.Location;

public class GeoCoordinate : IEquatable<GeoCoordinate>
{
    private double m_latitude = double.NaN;
    private double m_longitude = double.NaN;
    private double m_altitude = double.NaN;
    private double m_verticalAccuracy = double.NaN;
    private double m_horizontalAccuracy = double.NaN;
    private double m_speed = double.NaN;
    private double m_course = double.NaN;

    public static readonly GeoCoordinate Unknown = new();

    internal CivicAddress m_address = CivicAddress.Unknown;

    #region Constructors

    //
    // private constructor for creating single instance of GeoCoordinate.Unknown
    //
    public GeoCoordinate()
    {
    }

    public GeoCoordinate(double latitude, double longitude)
        : this(latitude, longitude, double.NaN)
    {
    }

    public GeoCoordinate(double latitude, double longitude, double altitude)
        : this(latitude, longitude, altitude, double.NaN, double.NaN, double.NaN, double.NaN)
    {
    }

    public GeoCoordinate(double latitude, double longitude, double altitude,
        double horizontalAccuracy, double verticalAccuracy, double speed, double course)
    {
        Latitude = latitude;
        Longitude = longitude;

        Altitude = altitude;

        HorizontalAccuracy = horizontalAccuracy;
        VerticalAccuracy = verticalAccuracy;

        Speed = speed;
        Course = course;
    }

    #endregion

    #region Properties

    public double Latitude
    {
        get => m_latitude;
        set
        {
            if (value > 90.0 || value < -90.0)
                throw new ArgumentOutOfRangeException("Latitude",
                    SR.GetString(SR.Argument_MustBeInRangeNegative90to90));
            m_latitude = value;
        }
    }

    public double Longitude
    {
        get => m_longitude;
        set
        {
            if (value > 180.0 || value < -180.0)
                throw new ArgumentOutOfRangeException("Longitude",
                    SR.GetString(SR.Argument_MustBeInRangeNegative180To180));
            m_longitude = value;
        }
    }

    public double Altitude
    {
        get => m_altitude;

        set => m_altitude = value;
    }

    public double HorizontalAccuracy
    {
        get => m_horizontalAccuracy;
        set
        {
            if (value < 0.0)
                throw new ArgumentOutOfRangeException("HorizontalAccuracy",
                    SR.GetString(SR.Argument_MustBeNonNegative));
            m_horizontalAccuracy = value == 0.0 ? double.NaN : value;
        }
    }

    public double VerticalAccuracy
    {
        get => m_verticalAccuracy;
        set
        {
            if (value < 0.0)
                throw new ArgumentOutOfRangeException("VerticalAccuracy", SR.GetString(SR.Argument_MustBeNonNegative));
            m_verticalAccuracy = value == 0.0 ? double.NaN : value;
        }
    }

    public double Speed
    {
        get => m_speed;
        set
        {
            if (value < 0.0)
                throw new ArgumentOutOfRangeException("speed", SR.GetString(SR.Argument_MustBeNonNegative));
            m_speed = value;
        }
    }

    public double Course
    {
        get => m_course;
        set
        {
            if (value < 0.0 || value > 360.0)
                throw new ArgumentOutOfRangeException("course", SR.GetString(SR.Argument_MustBeInRangeZeroTo360));
            m_course = value;
        }
    }

    public bool IsUnknown => Equals(Unknown);

    #endregion

    #region Methods

    public double GetDistanceTo(GeoCoordinate other)
    {
        //  The Haversine formula according to Dr. Math.
        //  http://mathforum.org/library/drmath/view/51879.html

        //  dlon = lon2 - lon1
        //  dlat = lat2 - lat1
        //  a = (sin(dlat/2))^2 + cos(lat1) * cos(lat2) * (sin(dlon/2))^2
        //  c = 2 * atan2(sqrt(a), sqrt(1-a)) 
        //  d = R * c

        //  Where
        //    * dlon is the change in longitude
        //    * dlat is the change in latitude
        //    * c is the great circle distance in Radians.
        //    * R is the radius of a spherical Earth.
        //    * The locations of the two points in 
        //        spherical coordinates (longitude and 
        //        latitude) are lon1,lat1 and lon2, lat2.

        if (double.IsNaN(Latitude) || double.IsNaN(Longitude) ||
            double.IsNaN(other.Latitude) || double.IsNaN(other.Longitude))
            throw new ArgumentException(SR.GetString(SR.Argument_LatitudeOrLongitudeIsNotANumber));

        var dDistance = double.NaN;

        var dLat1 = Latitude * (Math.PI / 180.0);
        var dLon1 = Longitude * (Math.PI / 180.0);
        var dLat2 = other.Latitude * (Math.PI / 180.0);
        var dLon2 = other.Longitude * (Math.PI / 180.0);

        var dLon = dLon2 - dLon1;
        var dLat = dLat2 - dLat1;

        // Intermediate result a.
        var a = Math.Pow(Math.Sin(dLat / 2.0), 2.0) +
                Math.Cos(dLat1) * Math.Cos(dLat2) *
                Math.Pow(Math.Sin(dLon / 2.0), 2.0);

        // Intermediate result c (great circle distance in Radians).
        var c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

        // Distance.
        const double kEarthRadiusMs = 6376500;
        dDistance = kEarthRadiusMs * c;

        return dDistance;
    }

    #endregion

    #region Object overrides

    public override int GetHashCode()
    {
        return Latitude.GetHashCode() ^ Longitude.GetHashCode();
    }

    /// <summary>
    /// Object.Equals. Calls into IEquatable.Equals
    /// </summary>
    /// <param name="obj"></param>
    /// <returns></returns>
    public override bool Equals(object obj)
    {
        if (!(obj is GeoCoordinate)) return base.Equals(obj);
        return Equals(obj as GeoCoordinate);
    }

    /// <summary>
    /// This override is for debugging purpose only
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        if (this == Unknown)
            return "Unknown";
        else
            return Latitude.ToString("G", CultureInfo.InvariantCulture) + ", " +
                   Longitude.ToString("G", CultureInfo.InvariantCulture);
    }

    #endregion

    #region IEquatable

    public bool Equals(GeoCoordinate other)
    {
        if (ReferenceEquals(other, null)) return false;
        return Latitude.Equals(other.Latitude) && Longitude.Equals(other.Longitude);
    }

    #endregion

    #region Public static operators

    public static bool operator ==(GeoCoordinate left, GeoCoordinate right)
    {
        if (ReferenceEquals(left, null)) return ReferenceEquals(right, null);
        return left.Equals(right);
    }

    public static bool operator !=(GeoCoordinate left, GeoCoordinate right)
    {
        return !(left == right);
    }

    #endregion
}