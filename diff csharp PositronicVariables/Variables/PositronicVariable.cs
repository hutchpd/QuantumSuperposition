         public void Assign(QuBit<T> qb)
         {
             if (qb is null)
                 throw new ArgumentNullException(nameof(qb));
 
             // Ensure the qubit has been collapsed at least once, or else later maths
             // operations might throw a fit and fall over.
             qb.Any();
 
             // If we saw `.State` read in this forward half-cycle, try to infer +k / *k / /k from before?after.
             if (_runtime.Entropy > 0 && InConvergenceLoop && isValueType && timeline.Count == 1 && _sawStateReadThisForward)
             {
                 // because double-logging is how timelines split and universes cry.
                 var top = QuantumLedgerOfRegret.Peek();
                 if (top is not AdditionOperation<T> && top is not MultiplicationOperation<T> && top is not DivisionOperation<T>)
                 {
                     // Single-value qubit? Great. At least something is behaving.
                     var incomingPreview = qb.ToCollapsedValues().Take(2).ToArray();
                     if (incomingPreview.Length == 1)
                     {
                         try
                         {
                             var before = GetCurrentQBit().ToCollapsedValues().First();
                             dynamic a = incomingPreview[0];
                             dynamic b = before;
                             TryInferAndRecordArithmetic((T)b, (T)a);
                         }
                         catch
                         {
                             // T is probably a string, or a cat, or something equally uncooperative
                             // in which case good luck!
                         }
                     }
                 }
-                // consume the read marker so it cannot leak to subsequent writes
-                _sawStateReadThisForward = false;
             }
-            else
-            {
-                _sawStateReadThisForward = false;
-            }
+            
+            // Always consume the read marker after processing
+            _sawStateReadThisForward = false;
 
             // And now we quietly overwrite or merge, as though time is a spreadsheet.
             ReplaceOrAppendOrUnify(qb, replace: true);
         }