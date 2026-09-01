using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SendEpsGrandia
{
    partial class EPSGenerate
    {
        private System.ComponentModel.IContainer components = null;


        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SendDataEPSJob = new System.ComponentModel.BackgroundWorker();
            this.SendComprobantesEPSJob = new System.ComponentModel.BackgroundWorker();
            this.RestaurarEstadosComprobanteEPS = new System.ComponentModel.BackgroundWorker();
            this.LiberarPolizasEPS = new System.ComponentModel.BackgroundWorker();


            this.SendDataEPSJob.DoWork += new System.ComponentModel.DoWorkEventHandler(this.SendDataEPSJob_DoWork);
            this.SendComprobantesEPSJob.DoWork += new System.ComponentModel.DoWorkEventHandler(this.SendComprobantesEPSJob_DoWork);
            this.RestaurarEstadosComprobanteEPS.DoWork += new System.ComponentModel.DoWorkEventHandler(this.RestaurarEstadosComprobanteEPS_DoWork);
            this.LiberarPolizasEPS.DoWork += new System.ComponentModel.DoWorkEventHandler(this.LiberarPolizasEPS_DoWork);

            this.ServiceName = "SendEPSGrandia";
        }

        private System.ComponentModel.BackgroundWorker SendDataEPSJob;
        private System.ComponentModel.BackgroundWorker SendComprobantesEPSJob;
        private System.ComponentModel.BackgroundWorker RestaurarEstadosComprobanteEPS;
        private System.ComponentModel.BackgroundWorker LiberarPolizasEPS;

    }
}
