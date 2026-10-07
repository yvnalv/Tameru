import { describe, it, expect } from 'vitest';
import { mount } from '@vue/test-utils';
import { createI18n } from 'vue-i18n';
import en from '@/i18n/locales/en';
import id from '@/i18n/locales/id';
import MoneyInput from '@/components/ui/MoneyInput.vue';

const i18n = createI18n({ legacy: false, locale: 'en', messages: { en, id } });

/**
 * Guards the money-entry control. The original defect (CHG-0043) was an HTML5 `step` constraint on
 * a raw `<input type="number">`: the browser rejected any amount that did not land on a 1.000
 * boundary. These tests pin the properties that make that class of bug impossible here — the
 * control is a text field with no numeric step, and it accepts arbitrary amounts including
 * decimals.
 */
describe('MoneyInput', () => {
  const mountInput = (modelValue = 0) =>
    mount(MoneyInput, {
      props: { modelValue, showChips: false },
      global: { plugins: [i18n] },
    });

  it('is a text field with no step/min constraint the browser can reject', () => {
    const input = mountInput().find('input');

    expect(input.attributes('type')).toBe('text');
    expect(input.attributes('step')).toBeUndefined();
    expect(input.attributes('min')).toBeUndefined();
    expect(input.attributes('max')).toBeUndefined();
  });

  it.each([
    ['12313', 12313],
    ['1', 1],
    ['999999999', 999999999],
    ['12.313', 12313],
  ])('accepts the arbitrary amount %s', async (typed, expected) => {
    const wrapper = mountInput();
    const input = wrapper.find('input');

    await input.setValue(typed);

    const emitted = wrapper.emitted('update:modelValue');
    expect(emitted).toBeTruthy();
    expect(emitted!.at(-1)![0]).toBe(expected);
  });

  it('parses Indonesian shorthand into a full amount', async () => {
    const wrapper = mountInput();

    await wrapper.find('input').setValue('50k');

    expect(wrapper.emitted('update:modelValue')!.at(-1)![0]).toBe(50000);
  });

  it('parses an additive expression', async () => {
    const wrapper = mountInput();

    await wrapper.find('input').setValue('25k+15k');

    expect(wrapper.emitted('update:modelValue')!.at(-1)![0]).toBe(40000);
  });

  it('emits 0 when cleared rather than leaving a stale amount', async () => {
    const wrapper = mountInput(75000);

    await wrapper.find('input').setValue('');

    expect(wrapper.emitted('update:modelValue')!.at(-1)![0]).toBe(0);
  });
});
