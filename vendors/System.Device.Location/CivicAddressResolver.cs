// ==++==
// 
//   Copyright (c) Microsoft Corporation.  All rights reserved.
// 
// ==--==
/*=============================================================================
**
** Class: CivicAddress
**
** Purpose: Represents a CivicAddress object
**
=============================================================================*/

using System;
using System.ComponentModel;
using System.Globalization;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics.CodeAnalysis;
using Strings.Resources;

namespace System.Device.Location;

public partial class CivicAddress
{
    public static readonly CivicAddress Unknown = new();

    //
    // private construcotr for creating single instance of CivicAddress.Unknown
    //
    public CivicAddress()
    {
        AddressLine1 = string.Empty;
        AddressLine2 = string.Empty;
        Building = string.Empty;
        City = string.Empty;
        CountryRegion = string.Empty;
        FloorLevel = string.Empty;
        PostalCode = string.Empty;
        StateProvince = string.Empty;
    }

    public CivicAddress(string addressLine1, string addressLine2, string building, string city, string countryRegion,
        string floorLevel, string postalCode, string stateProvince)
        : this()
    {
        var hasField = false;

        if (addressLine1 != null && addressLine1 != string.Empty)
        {
            hasField = true;
            AddressLine1 = addressLine1;
        }

        if (addressLine2 != null && addressLine2 != string.Empty)
        {
            hasField = true;
            AddressLine2 = addressLine2;
        }

        if (building != null && building != string.Empty)
        {
            hasField = true;
            Building = building;
        }

        if (city != null && city != string.Empty)
        {
            hasField = true;
            City = city;
        }

        if (countryRegion != null && countryRegion != string.Empty)
        {
            hasField = true;
            CountryRegion = countryRegion;
        }

        if (floorLevel != null && floorLevel != string.Empty)
        {
            hasField = true;
            FloorLevel = floorLevel;
        }

        if (postalCode != null && postalCode != string.Empty)
        {
            hasField = true;
            PostalCode = postalCode;
        }

        if (stateProvince != null && stateProvince != string.Empty)
        {
            hasField = true;
            StateProvince = stateProvince;
        }

        if (!hasField) throw new ArgumentException(SR.GetString(SR.Argument_RequiresAtLeastOneNonEmptyStringParameter));
    }

    public string AddressLine1 { get; set; }
    public string AddressLine2 { get; set; }
    public string Building { get; set; }
    public string City { get; set; }
    public string CountryRegion { get; set; }
    public string FloorLevel { get; set; }
    public string PostalCode { get; set; }
    public string StateProvince { get; set; }

    public bool IsUnknown =>
        string.IsNullOrEmpty(AddressLine1) && string.IsNullOrEmpty(AddressLine2) &&
        string.IsNullOrEmpty(Building) && string.IsNullOrEmpty(City) && string.IsNullOrEmpty(CountryRegion) &&
        string.IsNullOrEmpty(FloorLevel) && string.IsNullOrEmpty(PostalCode) && string.IsNullOrEmpty(StateProvince);
}

public interface ICivicAddressResolver
{
    CivicAddress ResolveAddress(GeoCoordinate coordinate);
    void ResolveAddressAsync(GeoCoordinate coordinate);
    event EventHandler<ResolveAddressCompletedEventArgs> ResolveAddressCompleted;
}

public class ResolveAddressCompletedEventArgs : AsyncCompletedEventArgs
{
    public ResolveAddressCompletedEventArgs(CivicAddress address, Exception error, bool cancelled, object userState)
        : base(error, cancelled, userState)
    {
        Address = address;
    }

    public CivicAddress Address { get; private set; }
}

public sealed class CivicAddressResolver : ICivicAddressResolver
{
    private SynchronizationContext m_synchronizationContext;

    public CivicAddressResolver()
    {
        if (SynchronizationContext.Current == null)
            //
            // Create a SynchronizationContext if there isn't one on calling thread
            //
            m_synchronizationContext = new SynchronizationContext();
        else
            m_synchronizationContext = SynchronizationContext.Current;
    }

    public CivicAddress ResolveAddress(GeoCoordinate coordinate)
    {
        if (coordinate == null) throw new ArgumentNullException("coordinate");

        if (coordinate.IsUnknown) throw new ArgumentException("coordinate");

        return coordinate.m_address;
    }

    public void ResolveAddressAsync(GeoCoordinate coordinate)
    {
        if (coordinate == null) throw new ArgumentNullException("coordinate");

        if (double.IsNaN(coordinate.Latitude) || double.IsNaN(coordinate.Longitude))
            throw new ArgumentException("coordinate");

        ThreadPool.QueueUserWorkItem(new WaitCallback(ResolveAddress), coordinate);
    }

    public event EventHandler<ResolveAddressCompletedEventArgs> ResolveAddressCompleted;

    private void OnResolveAddressCompleted(ResolveAddressCompletedEventArgs e)
    {
        var t = ResolveAddressCompleted;
        if (t != null) t(this, e);
    }

    /// <summary>Represents a callback to a protected virtual method that raises an event.</summary>
    /// <typeparam name="T">The <see cref="T:System.EventArgs"/> type identifying the type of object that gets raised with the event"/></typeparam>
    /// <param name="e">The <see cref="T:System.EventArgs"/> object that should be passed to a protected virtual method that raises the event.</param>
    private delegate void EventRaiser<T>(T e) where T : EventArgs;

    /// <summary>A helper method used by derived types that asynchronously raises an event on the application's desired thread.</summary>
    /// <typeparam name="T">The <see cref="T:System.EventArgs"/> type identifying the type of object that gets raised with the event"/></typeparam>
    /// <param name="callback">The protected virtual method that will raise the event.</param>
    /// <param name="e">The <see cref="T:System.EventArgs"/> object that should be passed to the protected virtual method raising the event.</param>
    private void PostEvent<T>(EventRaiser<T> callback, T e) where T : EventArgs
    {
        if (m_synchronizationContext != null)
            m_synchronizationContext.Post(delegate(object state) { callback((T)state); }, e);
    }

    //
    // Thread pool thread used to resolve civic address
    //
    private void ResolveAddress(object state)
    {
        var coordinate = state as GeoCoordinate;
        if (coordinate != null)
            PostEvent(OnResolveAddressCompleted,
                new ResolveAddressCompletedEventArgs(coordinate.m_address, null, false, null));
    }
}