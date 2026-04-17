using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GreenField.Data;
using GreenField.Models;

namespace GreenField.Controllers
{
    public class StampsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StampsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Stamps
        public async Task<IActionResult> Index()
        {
            return View(await _context.Stamps.ToListAsync());
        }

        // GET: Stamps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stamps = await _context.Stamps
                .FirstOrDefaultAsync(m => m.StampsId == id);
            if (stamps == null)
            {
                return NotFound();
            }

            return View(stamps);
        }

        // GET: Stamps/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Stamps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("StampsId,StampName,StampDescription")] Stamps stamps)
        {
            if (ModelState.IsValid)
            {
                _context.Add(stamps);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(stamps);
        }

        // GET: Stamps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stamps = await _context.Stamps.FindAsync(id);
            if (stamps == null)
            {
                return NotFound();
            }
            return View(stamps);
        }

        // POST: Stamps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("StampsId,StampName,StampDescription")] Stamps stamps)
        {
            if (id != stamps.StampsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(stamps);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StampsExists(stamps.StampsId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(stamps);
        }

        // GET: Stamps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var stamps = await _context.Stamps
                .FirstOrDefaultAsync(m => m.StampsId == id);
            if (stamps == null)
            {
                return NotFound();
            }

            return View(stamps);
        }

        // POST: Stamps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var stamps = await _context.Stamps.FindAsync(id);
            if (stamps != null)
            {
                _context.Stamps.Remove(stamps);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StampsExists(int id)
        {
            return _context.Stamps.Any(e => e.StampsId == id);
        }
    }
}
