import React from 'react';

export const App: React.FC = () => {
  return (
    <div className="min-h-screen flex flex-col justify-center items-center p-6 bg-slate-50">
      <div className="max-w-md w-full bg-white p-8 rounded-xl shadow-sm border border-slate-200 text-center">
        <h1 className="text-2xl font-bold text-slate-900 mb-2">SchoolOS</h1>
        <p className="text-sm text-slate-500 mb-6">
          Educational Operating Platform — Phase 0 Foundation
        </p>
        <div className="p-3 bg-emerald-50 text-emerald-800 rounded-lg text-xs font-medium">
          Frontend Shell Scaffolding Operational
        </div>
      </div>
    </div>
  );
};
