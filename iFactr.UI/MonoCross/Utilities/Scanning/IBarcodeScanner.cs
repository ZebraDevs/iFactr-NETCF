﻿using System;
using System.Collections.Generic;

namespace MonoCross.Utilities.Barcode
{
    public interface IBarcodeScanner
    {
        /// <summary>
        /// Enable the scanner for actual scanning, for example, a scanner may need to be enabled for automated
        /// scanner or to enable a button on the scanner for a form or on a particular field
        /// </summary>
        void Start();

        /// <summary>
        /// Disable the scanner entirely to avoid inadvertant button presses or power saving, etc
        /// </summary>
        void Stop();

        /// <summary>
        /// terminate the scanner, after this point the scanner interface will be invalid for use and the factory will
        /// have to be called to get a new interface to the scanner
        /// </summary>
        void Terminate();

        ///// <summary>
        ///// Event generated by the scanner once it is physically ready to use
        ///// </summary>
        //event EventHandler StatusChanged;

        ///// <summary>
        ///// 
        ///// </summary>
        //BarcodeScannerStatus Status { get; }

        /// <summary>
        /// Barcode returned on a scan
        /// </summary>
        event EventHandler<BarcodeScanEvent> BarcodeScan;

        // need to address sound in this layer possibilities 
        // - tone generators
        //   SetScanBeep(); 
        // - sound files (WAV, MP3, OGG, others)?
        //   SetScanSound();

        // need to address hardware button issues, setting, does it have, etc

        /// <summary>
        /// Method to determine if a particular barcode symbology is supported by the scanner
        /// </summary>
        /// <param name="symbology">Barcode Symbology</param>
        /// <returns>true if symbology is supported, false otherwire</returns>
        bool IsSymbologyAvailable(Symbology symbology);

        /// <summary>
        /// Method to enable a particular symbology
        /// </summary>
        /// <param name="symbology"></param>
        /// <returns></returns>
        bool EnableSymbology(Symbology symbology);

        /// <summary>
        /// Method to disable a particular symbology
        /// </summary>
        /// <param name="symbology"></param>
        /// <returns></returns>
        bool DisableSymbology(Symbology symbology);

        /// <summary>
        /// Method to test if a particular symbology is enabled
        /// </summary>
        /// <param name="symbology"></param>
        /// <returns></returns>
        bool IsSymbologyEnabled(Symbology symbology);

        /// <summary>
        /// Sets the symbologies to ONLY those specified, disabling all others
        /// </summary>
        /// <param name="symbologies">A list of the symbologies to enable</param>
        /// <returns>true on success, false on failure</returns>
        bool EnableSymbologies(IEnumerable<Symbology> symbologies);

        // possibly need to address a GUI button for the trigger?
    }
    public enum BarcodeScannerStatus
    {
        Connected,
        Connecting,
        Disconnected,
    }

    /// <summary>
    /// Barcode reader scan event
    /// </summary>
    public class BarcodeScanEvent : EventArgs
    {
        public BarcodeScanEvent() { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data">The barcode data</param>
        /// <param name="time">The time the scan took place, device time so may not be accurate</param>
        /// <param name="type">The symbology of the barcode scanned</param>
        public BarcodeScanEvent(string data, DateTime time, Symbology type)
        {
            Data = data;
            Time = time;
            Type = type;
        }

        /// <summary>
        /// The barcode scanned
        /// </summary>
        public string Data { get; set; }

        /// <summary>
        /// The time the scan took place, device time so may not be accurate
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// The symbology of the barcode scanned, not sure yet if this is applicable to all scanners, only some may return the type
        /// </summary>
        public Symbology Type { get; set; }
    }
}