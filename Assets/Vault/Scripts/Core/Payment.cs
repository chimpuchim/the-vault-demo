using System;
using System.Collections;
using UnityEngine;

namespace Vault
{
    public enum PaymentStatus { Approved, Declined, Cancelled }

    public class PaymentResult
    {
        public PaymentStatus status;
        public float amount;
        public string reference;
        public string message;
    }

    /// <summary>
    /// The seam for EFTPOS. The game only ever calls RequestPayment and Cancel; a terminal
    /// integration (Tyro, Linkly, Adyen...) implements this interface and the flow does not change.
    /// </summary>
    public interface IPaymentProvider
    {
        string DisplayName { get; }
        /// <summary>Starts a payment. onStatus receives progress text for the screen, onDone the outcome.</summary>
        IEnumerator RequestPayment(float amount, string currency, Action<string> onStatus, Action<PaymentResult> onDone);
        void Cancel();
    }

    /// <summary>Simulates a tap-and-go terminal: a short "present card" wait, then approval.</summary>
    public class MockPaymentProvider : IPaymentProvider
    {
        bool _cancelled;
        public string DisplayName => "Demo terminal (no real payment)";

        public IEnumerator RequestPayment(float amount, string currency, Action<string> onStatus, Action<PaymentResult> onDone)
        {
            _cancelled = false;
            onStatus?.Invoke("Present card or device");
            float t = 0;
            while (t < 1.8f && !_cancelled)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_cancelled)
            {
                onDone?.Invoke(new PaymentResult { status = PaymentStatus.Cancelled, amount = amount, message = "Cancelled" });
                yield break;
            }
            onStatus?.Invoke("Processing…");
            yield return new WaitForSecondsRealtime(0.9f);
            onDone?.Invoke(new PaymentResult
            {
                status = PaymentStatus.Approved,
                amount = amount,
                reference = "DEMO-" + DateTime.Now.ToString("HHmmss"),
                message = "Approved",
            });
        }

        public void Cancel() => _cancelled = true;
    }
}
