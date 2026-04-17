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
    public class ProducerStampsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProducerStampsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: ProducerStamps
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.ProducerStamps.Include(p => p.Producers).Include(p => p.Stamps);
            return View(await applicationDbContext.ToListAsync());
        }

        // GET: ProducerStamps/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producerStamps = await _context.ProducerStamps
                .Include(p => p.Producers)
                .Include(p => p.Stamps)
                .FirstOrDefaultAsync(m => m.ProducerStampsId == id);
            if (producerStamps == null)
            {
                return NotFound();
            }

            return View(producerStamps);
        }

        // GET: ProducerStamps/Create
        public IActionResult Create()
        {
            ViewData["ProducersId"] = new SelectList(_context.Producers, "ProducersId", "ProducersId");
            ViewData["StampsId"] = new SelectList(_context.Set<Stamps>(), "StampsId", "StampsId");
            return View();
        }

        // POST: ProducerStamps/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ProducerStampsId,ProducersId,StampsId")] ProducerStamps producerStamps)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producerStamps);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProducersId"] = new SelectList(_context.Producers, "ProducersId", "ProducersId", producerStamps.ProducersId);
            ViewData["StampsId"] = new SelectList(_context.Set<Stamps>(), "StampsId", "StampsId", producerStamps.StampsId);
            return View(producerStamps);
        }

        // GET: ProducerStamps/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producerStamps = await _context.ProducerStamps.FindAsync(id);
            if (producerStamps == null)
            {
                return NotFound();
            }
            ViewData["ProducersId"] = new SelectList(_context.Producers, "ProducersId", "ProducersId", producerStamps.ProducersId);
            ViewData["StampsId"] = new SelectList(_context.Set<Stamps>(), "StampsId", "StampsId", producerStamps.StampsId);
            return View(producerStamps);
        }

        // POST: ProducerStamps/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ProducerStampsId,ProducersId,StampsId")] ProducerStamps producerStamps)
        {
            if (id != producerStamps.ProducerStampsId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producerStamps);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProducerStampsExists(producerStamps.ProducerStampsId))
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
            ViewData["ProducersId"] = new SelectList(_context.Producers, "ProducersId", "ProducersId", producerStamps.ProducersId);
            ViewData["StampsId"] = new SelectList(_context.Set<Stamps>(), "StampsId", "StampsId", producerStamps.StampsId);
            return View(producerStamps);
        }

        // GET: ProducerStamps/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var producerStamps = await _context.ProducerStamps
                .Include(p => p.Producers)
                .Include(p => p.Stamps)
                .FirstOrDefaultAsync(m => m.ProducerStampsId == id);
            if (producerStamps == null)
            {
                return NotFound();
            }

            return View(producerStamps);
        }

        // POST: ProducerStamps/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producerStamps = await _context.ProducerStamps.FindAsync(id);
            if (producerStamps != null)
            {
                _context.ProducerStamps.Remove(producerStamps);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProducerStampsExists(int id)
        {
            return _context.ProducerStamps.Any(e => e.ProducerStampsId == id);
        }
    }
}
